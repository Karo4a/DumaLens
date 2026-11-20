using AutoMapper;
using DumaLens.Context;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Default;
using DumaLens.Entities.Enums;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.Contracts.WriteRepositories;
using DumaLens.Repositories.ReadRepositories;
using DumaLens.Repositories.WriteRepositories;
using DumaLens.Services.Auth;
using DumaLens.Services.Contracts;
using DumaLens.Services.Infrastructure;
using Xunit;

namespace DumaLens.Services.Tests.Infrastructure;

/// <summary>
/// База тестов auth-сервисов: одноразовая PostgreSQL-база на каждый тест (изоляция мутирующих
/// сценариев: ротация, отзыв сессий, счётчики попыток), реальные репозитории без моков (07-security.md §5)
/// </summary>
public abstract class AuthTestBase : IAsyncLifetime
{
    /// <summary>
    /// Начальный момент времени тестовых часов
    /// </summary>
    protected static readonly DateTimeOffset StartTime = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Ключ подписи JWT для тестов (32 байта, base64; не секрет production)
    /// </summary>
    public const string TestSigningKey = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";

    /// <summary>
    /// Пароль тестовых пользователей по умолчанию
    /// </summary>
    protected const string DefaultPassword = "Password123";

    private readonly DatabaseFixture fixture = new();
    private DumaLensContext? context;

    /// <summary>
    /// Контекст одноразовой базы
    /// </summary>
    protected DumaLensContext Context => context ?? throw new InvalidOperationException("База не инициализирована (PostgreSQL недоступен)");

    /// <summary>
    /// Управляемые часы
    /// </summary>
    protected TestDateTimeProvider Clock { get; } = new(StartTime);

    /// <summary>
    /// Настройки auth (итерации PBKDF2 — боевые 100 000, ADR-12).
    /// Grace-окно отключено по умолчанию: тесты отзыва детерминированы без сдвига часов;
    /// поведение окна проверяется отдельными тестами, задающими значение явно
    /// </summary>
    protected AuthOptions Options { get; } = new AuthOptions
    {
        SigningKey = TestSigningKey,
        RefreshReuseGraceSeconds = 0,
    };

    /// <summary>
    /// Маппер сервисного профиля
    /// </summary>
    protected IMapper Mapper { get; private set; } = null!;

    /// <summary>
    /// Репозиторий чтения пользователей
    /// </summary>
    protected IUserReadRepository UserRead { get; private set; } = null!;

    /// <summary>
    /// Репозиторий записи пользователей
    /// </summary>
    protected IUserWriteRepository UserWrite { get; private set; } = null!;

    /// <summary>
    /// Репозиторий чтения refresh-токенов
    /// </summary>
    protected IRefreshTokenReadRepository RefreshRead { get; private set; } = null!;

    /// <summary>
    /// Репозиторий записи refresh-токенов
    /// </summary>
    protected IRefreshTokenWriteRepository RefreshWrite { get; private set; } = null!;

    /// <summary>
    /// Сервис токенов
    /// </summary>
    protected ITokenService Tokens { get; private set; } = null!;

    /// <summary>
    /// Сервис аутентификации
    /// </summary>
    protected IAuthService Auth { get; private set; } = null!;

    /// <summary>
    /// Сервис управления пользователями
    /// </summary>
    protected IUserService Users { get; private set; } = null!;

    /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
    public async Task InitializeAsync()
    {
        await fixture.InitializeAsync();
        if (fixture.SkipReason != null)
        {
            return;
        }
        context = fixture.CreateContext();
        Mapper = new MapperConfiguration(profile => profile.AddProfile<ServiceProfile>()).CreateMapper();
        var validateService = new ValidateService();
        UserRead = new UserReadRepository(context);
        UserWrite = new UserWriteRepository(context, Clock);
        RefreshRead = new RefreshTokenReadRepository(context);
        RefreshWrite = new RefreshTokenWriteRepository(context, context, Clock);
        var roleRead = new UserRoleReadRepository(context);
        Tokens = new TokenService(Options, Clock);
        Auth = new AuthService(validateService, UserRead, UserWrite, RefreshRead, RefreshWrite, context, Tokens, Clock, Options, Mapper);
        Users = new UserService(validateService, UserRead, UserWrite, roleRead, RefreshRead, RefreshWrite, context, Clock, Options, Mapper);
    }

    /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
    public async Task DisposeAsync()
    {
        if (context != null)
        {
            await context.DisposeAsync();
        }
        await fixture.DisposeAsync();
    }

    /// <summary>
    /// Пропускает тест, если PostgreSQL недоступен (ADR-3)
    /// </summary>
    protected void SkipIfUnavailable()
        => Skip.If(fixture.SkipReason != null, fixture.SkipReason);

    /// <summary>
    /// Независимый сервис аутентификации на отдельном контексте той же базы: DbContext не потокобезопасен,
    /// конкурентные сценарии выполняют операции через независимые контексты (как разные запросы к Api)
    /// </summary>
    protected IAuthService CreateAuthOnNewContext(out DumaLensContext context)
    {
        context = fixture.CreateContext();
        var mapper = new MapperConfiguration(profile => profile.AddProfile<ServiceProfile>()).CreateMapper();
        var validateService = new ValidateService();
        var userRead = new UserReadRepository(context);
        var userWrite = new UserWriteRepository(context, Clock);
        var refreshRead = new RefreshTokenReadRepository(context);
        var refreshWrite = new RefreshTokenWriteRepository(context, context, Clock);
        var tokens = new TokenService(Options, Clock);
        return new AuthService(validateService, userRead, userWrite, refreshRead, refreshWrite, context, tokens, Clock, Options, mapper);
    }

    /// <summary>
    /// Независимый сервис пользователей на отдельном контексте той же базы (конкурентные сценарии)
    /// </summary>
    protected IUserService CreateUsersOnNewContext(out DumaLensContext context)
    {
        context = fixture.CreateContext();
        var mapper = new MapperConfiguration(profile => profile.AddProfile<ServiceProfile>()).CreateMapper();
        var validateService = new ValidateService();
        var userRead = new UserReadRepository(context);
        var userWrite = new UserWriteRepository(context, Clock);
        var roleRead = new UserRoleReadRepository(context);
        var refreshRead = new RefreshTokenReadRepository(context);
        var refreshWrite = new RefreshTokenWriteRepository(context, context, Clock);
        return new UserService(validateService, userRead, userWrite, roleRead, refreshRead, refreshWrite, context, Clock, Options, mapper);
    }

    /// <summary>
    /// Создаёт пользователя напрямую в базе (состояния для arrange: блокировка, временный пароль, метка)
    /// </summary>
    protected async Task<User> SeedUser(string login, Role role = Role.User, string password = DefaultPassword, bool blocked = false, bool temporary = false)
    {
        var salt = SecurityHelper.GenerateSalt();
        var user = new User
        {
            Login = login,
            LoginLowerCase = login.ToLowerInvariant(),
            Email = string.Empty,
            EmailLowerCase = string.Empty,
            Name = login,
            PasswordSalt = salt,
            PasswordHash = SecurityHelper.HashPassword(password, salt, Options.PasswordIterations),
            PasswordIsTemporary = temporary,
            SecurityStamp = Guid.NewGuid(),
            RoleId = UserRolesDefault.DefaultRoleToGuid(role),
            Blocked = blocked,
        };
        ((IDbWriter<User>)UserWrite).Add(user);
        await ((IUnitOfWork)Context).SaveChangesAsync();
        return user;
    }
}