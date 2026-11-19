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

namespace DumaLens.Services.Auth;

/// <inheritdoc cref="IAuthService"/>
/// <remarks>
/// Ротация refresh-токенов (ADR-13): старый токен отзывается, выпускается новая пара.
/// Повторное использование отозванного токена отзывает все сессии пользователя и меняет
/// метку безопасности (защита от кражи). Смена пароля и блокировка меняют метку безопасности.
/// </remarks>
public class AuthService : IAuthService, IServiceAnchor
{
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
            throw new DumaLensUnauthorizedException("Неверный логин или пароль");
        }
        if (user.Blocked)
        {
            throw new DumaLensUnauthorizedException("Учётная запись заблокирована");
        }
        var now = dateTimeProvider.UtcNow();
        if (IsLockedOut(user, now))
        {
            throw new DumaLensUnauthorizedException($"Слишком много неудачных попыток входа. Повторите через {options.LoginLockoutMinutes} минут");
        }
        if (!SecurityHelper.VerifyPassword(model.Password, user.PasswordSalt, user.PasswordHash, options.PasswordIterations))
        {
            await RegisterFailedAttempt(user, now, cancellationToken);
            throw new DumaLensUnauthorizedException("Неверный логин или пароль");
        }
        var entity = mapper.Map<User>(user);
        entity.LoginAttemptFailedCount = 0;
        entity.LastFailedLoginAt = null;
        userWriteRepository.Edit(entity);
        return await IssueSession(user, now, cancellationToken);
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
        if (stored.RevokedAt != null)
        {
            // Повторное использование отозванного токена — признак кражи: отзываем все сессии
            await RevokeAllSessions(stored.UserId, now, rotateStamp: true, cancellationToken);
            throw new DumaLensUnauthorizedException("Токен обновления отозван: все сессии завершены");
        }
        var user = await userReadRepository.GetById(stored.UserId, cancellationToken);
        if (user == null || user.Blocked)
        {
            await RevokeToken(stored, now, cancellationToken);
            throw new DumaLensUnauthorizedException("Учётная запись недоступна");
        }
        if (user.SecurityStamp != stored.SecurityStamp)
        {
            await RevokeToken(stored, now, cancellationToken);
            throw new DumaLensUnauthorizedException("Токен обновления отозван");
        }
        if (user.PasswordIsTemporary)
        {
            await RevokeToken(stored, now, cancellationToken);
            throw new DumaLensUnauthorizedException("Требуется смена временного пароля");
        }
        return await IssueSession(user, now, cancellationToken);
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
        if (!SecurityHelper.VerifyPassword(model.CurrentPassword, user.PasswordSalt, user.PasswordHash, options.PasswordIterations))
        {
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
        await RevokeAllSessions(userId, dateTimeProvider.UtcNow(), rotateStamp: false, cancellationToken);
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
            Id = Guid.NewGuid(),
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
}