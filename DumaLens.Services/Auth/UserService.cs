using AutoMapper;
using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.Contracts.WriteRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;
using DumaLens.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DumaLens.Services.Auth;

/// <inheritdoc cref="IUserService"/>
/// <remarks>
/// Управление пользователями доступно только Администратору (политика Api).
/// Смена роли, блокировка и сброс пароля меняют метку безопасности и отзывают сессии.
/// Последнего активного администратора нельзя лишить прав — защита от потери доступа;
/// проверка выполняется в serializable-транзакции с повтором, чтобы параллельное
/// разжалование двух последних администраторов не оставило систему без администратора.
/// </remarks>
public class UserService : IUserService, IServiceAnchor
{
    private const int MaxConcurrencyRetries = 2;

    private readonly IValidateService validateService;
    private readonly IUserReadRepository userReadRepository;
    private readonly IUserWriteRepository userWriteRepository;
    private readonly IUserRoleReadRepository userRoleReadRepository;
    private readonly IRefreshTokenReadRepository refreshTokenReadRepository;
    private readonly IRefreshTokenWriteRepository refreshTokenWriteRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly AuthOptions options;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UserService(
        IValidateService validateService,
        IUserReadRepository userReadRepository,
        IUserWriteRepository userWriteRepository,
        IUserRoleReadRepository userRoleReadRepository,
        IRefreshTokenReadRepository refreshTokenReadRepository,
        IRefreshTokenWriteRepository refreshTokenWriteRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        AuthOptions options,
        IMapper mapper)
    {
        this.validateService = validateService;
        this.userReadRepository = userReadRepository;
        this.userWriteRepository = userWriteRepository;
        this.userRoleReadRepository = userRoleReadRepository;
        this.refreshTokenReadRepository = refreshTokenReadRepository;
        this.refreshTokenWriteRepository = refreshTokenWriteRepository;
        this.unitOfWork = unitOfWork;
        this.dateTimeProvider = dateTimeProvider;
        this.options = options;
        this.mapper = mapper;
    }

    async Task<UserPageModel> IUserService.GetPage(UserListQueryModel query, CancellationToken cancellationToken)
    {
        await validateService.Validate(query, cancellationToken);
        var page = await userReadRepository.GetPage(query.Page, query.PageSize, cancellationToken);
        return mapper.Map<UserPageModel>(page);
    }

    async Task<UserListItemModel> IUserService.Create(CreateUserModel model, CancellationToken cancellationToken)
    {
        await validateService.Validate(model, cancellationToken);
        var login = model.Login.Trim();
        var existing = await userReadRepository.GetByLogin(login, cancellationToken);
        if (existing != null)
        {
            throw new DumaLensDuplicateException($"Логин «{login}» уже занят");
        }
        var email = model.Email?.Trim() ?? string.Empty;
        await EnsureEmailIsFree(email, excludeUserId: null, cancellationToken);
        // Токен роли гарантированно распознан валидатором
        var role = RoleHelper.Parse(model.Role)!.Value;
        var roleEntity = await userRoleReadRepository.GetByRole(role, cancellationToken)
            ?? throw new DumaLensNotFoundException($"Роль «{model.Role}» не найдена");
        var salt = SecurityHelper.GenerateSalt();
        var user = new User
        {
            Login = login,
            LoginLowerCase = login.ToLowerInvariant(),
            Email = email,
            EmailLowerCase = email.ToLowerInvariant(),
            Name = model.Name.Trim(),
            PasswordSalt = salt,
            PasswordHash = SecurityHelper.HashPassword(model.Password, salt, options.PasswordIterations),
            PasswordIsTemporary = true,
            SecurityStamp = Guid.NewGuid(),
            RoleId = roleEntity.Id,
        };
        userWriteRepository.Add(user);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
        {
            // Параллельное создание с тем же логином/почтой: уникальный индекс — источник истины
            throw TranslateDuplicate(ex, login, email);
        }
        // Перечитываем с ролью: навигация Role у созданной сущности не загружена
        var created = await userReadRepository.GetById(user.Id, cancellationToken)
            ?? throw new DumaLensNotFoundException("Созданный пользователь не найден");
        return mapper.Map<UserListItemModel>(created);
    }

    async Task<UserListItemModel> IUserService.Update(Guid id, UpdateUserModel model, CancellationToken cancellationToken)
    {
        await validateService.Validate(model, cancellationToken);
        var blocked = model.Blocked!.Value;
        var user = await userReadRepository.GetById(id, cancellationToken)
            ?? throw new DumaLensNotFoundException("Пользователь не найден");
        var role = RoleHelper.Parse(model.Role)!.Value;
        var roleEntity = await userRoleReadRepository.GetByRole(role, cancellationToken)
            ?? throw new DumaLensNotFoundException($"Роль «{model.Role}» не найдена");
        var email = model.Email?.Trim() ?? string.Empty;
        await EnsureEmailIsFree(email, id, cancellationToken);
        // Последний активный администратор не может потерять права (защита от потери доступа)
        var losesAdminRights = user.Role == Role.Administrator && !user.Blocked
            && (role != Role.Administrator || blocked);

        async Task ApplyAndSave()
        {
            var entity = mapper.Map<User>(user);
            entity.Name = model.Name.Trim();
            entity.Email = email;
            entity.EmailLowerCase = email.ToLowerInvariant();
            entity.RoleId = roleEntity.Id;
            entity.Blocked = blocked;
            entity.Note = model.Note;
            var sessionsInvalidated = entity.RoleId != user.RoleId || entity.Blocked != user.Blocked;
            if (sessionsInvalidated)
            {
                entity.SecurityStamp = Guid.NewGuid();
            }
            userWriteRepository.Edit(entity);
            if (sessionsInvalidated)
            {
                await RevokeActiveSession(id, dateTimeProvider.UtcNow(), cancellationToken);
            }
            else
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        if (losesAdminRights)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    // Serializable: параллельное разжалование двух последних админов даёт конфликт
                    // сериализации одному из запросов; повтор перечитывает счётчик и отклоняет второй
                    await using var transaction = await ((DbContext)unitOfWork).Database
                        .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                    var otherAdmins = await userReadRepository.CountActiveAdministrators(id, cancellationToken);
                    if (otherAdmins == 0)
                    {
                        throw new DumaLensValidationException([InvalidateItemModel.New(string.Empty, "Нельзя лишить прав последнего активного администратора")]);
                    }
                    await ApplyAndSave();
                    await transaction.CommitAsync(cancellationToken);
                    break;
                }
                catch (Exception ex) when (attempt < MaxConcurrencyRetries && PostgresErrors.IsSerializationFailure(ex))
                {
                    ((DbContext)unitOfWork).ChangeTracker.Clear();
                }
                catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
                {
                    throw TranslateDuplicate(ex, user.Login, email);
                }
            }
        }
        else
        {
            try
            {
                await ApplyAndSave();
            }
            catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
            {
                throw TranslateDuplicate(ex, user.Login, email);
            }
        }
        user.Name = model.Name.Trim();
        user.Email = email;
        user.Role = role;
        user.RoleId = roleEntity.Id;
        user.Blocked = blocked;
        user.Note = model.Note;
        return mapper.Map<UserListItemModel>(user);
    }

    async Task<ResetPasswordResultModel> IUserService.ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var user = await userReadRepository.GetById(id, cancellationToken)
            ?? throw new DumaLensNotFoundException("Пользователь не найден");
        var temporaryPassword = SecurityHelper.GenerateTemporaryPassword();
        var entity = mapper.Map<User>(user);
        entity.PasswordSalt = SecurityHelper.GenerateSalt();
        entity.PasswordHash = SecurityHelper.HashPassword(temporaryPassword, entity.PasswordSalt, options.PasswordIterations);
        entity.PasswordIsTemporary = true;
        entity.SecurityStamp = Guid.NewGuid();
        entity.LoginAttemptFailedCount = 0;
        entity.LastFailedLoginAt = null;
        userWriteRepository.Edit(entity);
        await RevokeActiveSession(id, dateTimeProvider.UtcNow(), cancellationToken);
        return new ResetPasswordResultModel
        {
            TemporaryPassword = temporaryPassword,
        };
    }

    private async Task EnsureEmailIsFree(string email, Guid? excludeUserId, CancellationToken cancellationToken)
    {
        if (email.Length == 0)
        {
            return;
        }
        var existing = await userReadRepository.GetByEmail(email, cancellationToken);
        if (existing != null && existing.Id != excludeUserId)
        {
            throw new DumaLensDuplicateException($"Электронная почта «{email}» уже используется");
        }
    }

    private async Task RevokeActiveSession(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await refreshTokenReadRepository.GetActiveByUserId(userId, cancellationToken);
        if (active != null)
        {
            var entity = mapper.Map<RefreshToken>(active);
            entity.RevokedAt = now;
            refreshTokenWriteRepository.Edit(entity);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DumaLensDuplicateException TranslateDuplicate(DbUpdateException ex, string login, string email)
    {
        var constraintName = (ex.InnerException as Npgsql.PostgresException)?.ConstraintName;
        return constraintName == "IX_User_EmailLowerCase"
            ? new DumaLensDuplicateException($"Электронная почта «{email}» уже используется")
            : new DumaLensDuplicateException($"Логин «{login}» уже занят");
    }
}