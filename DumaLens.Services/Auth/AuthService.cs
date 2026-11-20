using AutoMapper;
using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.Contracts.WriteRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;
using DumaLens.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Services.Auth;

/// <inheritdoc cref="IAuthService"/>
/// <remarks>
/// Ротация refresh-токенов (ADR-13): старый токен отзывается, выпускается новая пара.
/// Повторное использование отозванного токена вне grace-окна отзывает все сессии пользователя
/// и меняет метку безопасности (защита от кражи); внутри окна — гонка параллельных обновлений,
/// выпускается новая пара. Смена пароля и блокировка меняют метку безопасности.
/// Конкурентные login/refresh одного пользователя сериализуются повтором на уникальном индексе.
/// </remarks>
public class AuthService : IAuthService, IServiceAnchor
{
    private const int MaxConcurrencyRetries = 2;
    private const string GenericLoginError = "Неверный логин или пароль";

    private readonly IValidateService validateService;
    private readonly IUserReadRepository userReadRepository;
    private readonly IUserWriteRepository userWriteRepository;
    private readonly IRefreshTokenReadRepository refreshTokenReadRepository;
    private readonly IRefreshTokenWriteRepository refreshTokenWriteRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ITokenService tokenService;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly AuthOptions options;
    private readonly IMapper mapper;
    private (string Salt, string Hash)? dummyCredentials;

    /// <summary>
    /// Конструктор
    /// </summary>
    public AuthService(
        IValidateService validateService,
        IUserReadRepository userReadRepository,
        IUserWriteRepository userWriteRepository,
        IRefreshTokenReadRepository refreshTokenReadRepository,
        IRefreshTokenWriteRepository refreshTokenWriteRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        AuthOptions options,
        IMapper mapper)
    {
        this.validateService = validateService;
        this.userReadRepository = userReadRepository;
        this.userWriteRepository = userWriteRepository;
        this.refreshTokenReadRepository = refreshTokenReadRepository;
        this.refreshTokenWriteRepository = refreshTokenWriteRepository;
        this.unitOfWork = unitOfWork;
        this.tokenService = tokenService;
        this.dateTimeProvider = dateTimeProvider;
        this.options = options;
        this.mapper = mapper;
    }

    async Task<AuthResultModel> IAuthService.Login(LoginModel model, CancellationToken cancellationToken)
    {
        await validateService.Validate(model, cancellationToken);
        var user = await userReadRepository.GetByLogin(model.Login.Trim(), cancellationToken);
        if (user == null)
        {
            BurnDummyVerification(model.Password);
            throw new DumaLensUnauthorizedException(GenericLoginError);
        }
        // Единое сообщение для всех причин отказа: существование и состояние учётной записи не раскрываются
        if (user.Blocked)
        {
            throw new DumaLensUnauthorizedException(GenericLoginError);
        }
        var now = dateTimeProvider.UtcNow();
        if (IsLockedOut(user, now))
        {
            throw new DumaLensUnauthorizedException(GenericLoginError);
        }
        if (!SecurityHelper.VerifyPassword(model.Password, user.PasswordSalt, user.PasswordHash, options.PasswordIterations))
        {
            await RegisterFailedAttempt(user, now, cancellationToken);
            throw new DumaLensUnauthorizedException(GenericLoginError);
        }
        return await IssueSessionWithRetry(user, now, resetAttempts: true, cancellationToken);
    }

    async Task<AuthResultModel> IAuthService.Refresh(RefreshRequestModel model, CancellationToken cancellationToken)
    {
        await validateService.Validate(model, cancellationToken);
        var tokenHash = SecurityHelper.HashRefreshToken(model.RefreshToken);
        var stored = await refreshTokenReadRepository.GetByTokenHash(tokenHash, cancellationToken);
        if (stored == null)
        {
            throw new DumaLensUnauthorizedException("Токен обновления недействителен");
        }
        var now = dateTimeProvider.UtcNow();
        if (stored.ExpiresAt <= now)
        {
            throw new DumaLensUnauthorizedException("Срок действия токена обновления истёк");
        }
        var user = await userReadRepository.GetById(stored.UserId, cancellationToken);
        if (user == null)
        {
            throw new DumaLensUnauthorizedException("Учётная запись недоступна");
        }
        if (stored.RevokedAt != null)
        {
            var grace = TimeSpan.FromSeconds(options.RefreshReuseGraceSeconds);
            if (now - stored.RevokedAt.Value < grace && stored.SecurityStamp == user.SecurityStamp && !user.Blocked)
            {
                // Гонка параллельных обновлений (вкладки/устройства с общим хранилищем токенов):
                // токен отозван только что и метка актуальна — выпускаем новую пару вместо отзыва сессий
                return await IssueSessionWithRetry(user, now, resetAttempts: false, cancellationToken);
            }
            // Повторное использование отозванного токена вне grace-окна — признак кражи
            await RevokeAllSessions(stored.UserId, now, rotateStamp: true, cancellationToken);
            throw new DumaLensUnauthorizedException("Токен обновления отозван: все сессии завершены");
        }
        if (user.Blocked)
        {
            await RevokeToken(stored, now, cancellationToken);
            throw new DumaLensUnauthorizedException("Учётная запись недоступна");
        }
        if (user.SecurityStamp != stored.SecurityStamp)
        {
            await RevokeToken(stored, now, cancellationToken);
            throw new DumaLensUnauthorizedException("Токен обновления отозван");
        }
        // Временный пароль не блокирует обновление: принуждение смены выполняется авторизацией Api
        // (клейм pwd_temp), иначе смена пароля после истечения access-токена невозможна
        return await IssueSessionWithRetry(user, now, resetAttempts: false, cancellationToken);
    }

    async Task IAuthService.Logout(Guid userId, CancellationToken cancellationToken)
    {
        var active = await refreshTokenReadRepository.GetActiveByUserId(userId, cancellationToken);
        if (active != null)
        {
            await RevokeToken(active, dateTimeProvider.UtcNow(), cancellationToken);
        }
    }

    async Task IAuthService.ChangePassword(Guid userId, ChangePasswordModel model, CancellationToken cancellationToken)
    {
        await validateService.Validate(model, cancellationToken);
        var user = await userReadRepository.GetById(userId, cancellationToken)
            ?? throw new DumaLensNotFoundException("Пользователь не найден");
        var now = dateTimeProvider.UtcNow();
        if (user.Blocked)
        {
            throw new DumaLensUnauthorizedException("Учётная запись заблокирована");
        }
        if (IsLockedOut(user, now))
        {
            throw new DumaLensUnauthorizedException($"Слишком много неудачных попыток. Повторите через {options.LoginLockoutMinutes} минут");
        }
        if (!SecurityHelper.VerifyPassword(model.CurrentPassword, user.PasswordSalt, user.PasswordHash, options.PasswordIterations))
        {
            // Неудачные попытки смены пароля учитываются в том же счётчике блокировки, что и вход
            await RegisterFailedAttempt(user, now, cancellationToken);
            throw new DumaLensUnauthorizedException("Неверный текущий пароль");
        }
        var entity = mapper.Map<User>(user);
        entity.PasswordSalt = SecurityHelper.GenerateSalt();
        entity.PasswordHash = SecurityHelper.HashPassword(model.NewPassword, entity.PasswordSalt, options.PasswordIterations);
        entity.PasswordIsTemporary = false;
        entity.SecurityStamp = Guid.NewGuid();
        entity.LoginAttemptFailedCount = 0;
        entity.LastFailedLoginAt = null;
        userWriteRepository.Edit(entity);
        // Смена метки безопасности инвалидирует все сессии; SaveChanges один на всю операцию
        await RevokeAllSessions(userId, now, rotateStamp: false, cancellationToken);
    }

    async Task<UserProfileModel> IAuthService.GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userReadRepository.GetById(userId, cancellationToken)
            ?? throw new DumaLensNotFoundException("Пользователь не найден");
        return mapper.Map<UserProfileModel>(user);
    }

    private bool IsLockedOut(UserDbModel user, DateTimeOffset now)
        => user.LoginAttemptFailedCount >= options.MaxFailedLoginAttempts
            && user.LastFailedLoginAt.HasValue
            && now < user.LastFailedLoginAt.Value.AddMinutes(options.LoginLockoutMinutes);

    private async Task RegisterFailedAttempt(UserDbModel user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<User>(user);
        var windowExpired = !user.LastFailedLoginAt.HasValue
            || now >= user.LastFailedLoginAt.Value.AddMinutes(options.LoginLockoutMinutes);
        entity.LoginAttemptFailedCount = windowExpired ? 1 : user.LoginAttemptFailedCount + 1;
        entity.LastFailedLoginAt = now;
        userWriteRepository.Edit(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResultModel> IssueSessionWithRetry(UserDbModel user, DateTimeOffset now, bool resetAttempts, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                if (resetAttempts)
                {
                    var entity = mapper.Map<User>(user);
                    entity.LoginAttemptFailedCount = 0;
                    entity.LastFailedLoginAt = null;
                    userWriteRepository.Edit(entity);
                }
                return await IssueSession(user, now, cancellationToken);
            }
            catch (DbUpdateException ex) when (attempt < MaxConcurrencyRetries && PostgresErrors.IsUniqueViolation(ex))
            {
                // Параллельный login/refresh уже занял уникальный индекс активной сессии:
                // сбрасываем отслеживание и перечитываем состояние заново
                ((DbContext)unitOfWork).ChangeTracker.Clear();
            }
        }
    }

    private async Task<AuthResultModel> IssueSession(UserDbModel user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var previous = await refreshTokenReadRepository.GetActiveByUserId(user.Id, cancellationToken);
        if (previous != null)
        {
            var previousEntity = mapper.Map<RefreshToken>(previous);
            previousEntity.RevokedAt = now;
            refreshTokenWriteRepository.Edit(previousEntity);
        }
        var rawToken = SecurityHelper.GenerateRefreshToken();
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = SecurityHelper.HashRefreshToken(rawToken),
            SecurityStamp = user.SecurityStamp,
            CreatedAt = now,
            ExpiresAt = now.AddDays(options.RefreshTokenLifetimeDays),
        };
        refreshTokenWriteRepository.Add(refresh);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AuthResultModel
        {
            AccessToken = tokenService.CreateAccessToken(user),
            RefreshToken = rawToken,
            User = mapper.Map<UserProfileModel>(user),
        };
    }

    private async Task RevokeToken(RefreshTokenDbModel token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<RefreshToken>(token);
        entity.RevokedAt = now;
        refreshTokenWriteRepository.Edit(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeAllSessions(Guid userId, DateTimeOffset now, bool rotateStamp, CancellationToken cancellationToken)
    {
        var active = await refreshTokenReadRepository.GetActiveByUserId(userId, cancellationToken);
        if (active != null)
        {
            var entity = mapper.Map<RefreshToken>(active);
            entity.RevokedAt = now;
            refreshTokenWriteRepository.Edit(entity);
        }
        if (rotateStamp)
        {
            var user = await userReadRepository.GetById(userId, cancellationToken);
            if (user != null)
            {
                var entity = mapper.Map<User>(user);
                entity.SecurityStamp = Guid.NewGuid();
                userWriteRepository.Edit(entity);
            }
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void BurnDummyVerification(string password)
    {
        // Неизвестный логин проверяется против фиктивного хэша: время ответа не выдаёт существование пользователя
        if (dummyCredentials == null)
        {
            var salt = SecurityHelper.GenerateSalt();
            dummyCredentials = (salt, SecurityHelper.HashPassword("dummy-password", salt, options.PasswordIterations));
        }
        var (dummySalt, dummyHash) = dummyCredentials.Value;
        SecurityHelper.VerifyPassword(password, dummySalt, dummyHash, options.PasswordIterations);
    }
}