using AutoMapper;
using DumaLens.Api.Infrastructure.Authorization;
using DumaLens.Api.Models.Auth;
using DumaLens.Api.Models.Exceptions;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Аутентификация: вход, обновление и отзыв сессий, профиль, смена пароля (07-security.md §2.4)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowTemporaryPassword]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Имя политики rate limit эндпоинта логина (фиксированное окно на IP)
    /// </summary>
    public const string LoginRateLimitPolicy = "LoginRateLimit";

    /// <summary>
    /// Имя политики rate limit эндпоинта смены пароля (фиксированное окно на IP, отдельный бюджет)
    /// </summary>
    public const string ChangePasswordRateLimitPolicy = "ChangePasswordRateLimit";

    private readonly IAuthService authService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public AuthController(IAuthService authService, IMapper mapper)
    {
        this.authService = authService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Вход по логину и паролю: пара токенов (access JWT + refresh) и профиль
    /// </summary>
    /// <param name="request">Логин и пароль</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    [ProducesResponseType(typeof(AuthResponseApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequestApiModel request, CancellationToken cancellationToken)
    {
        var result = await authService.Login(mapper.Map<LoginModel>(request), cancellationToken);
        return Ok(mapper.Map<AuthResponseApiModel>(result));
    }

    /// <summary>
    /// Обновление пары токенов по refresh-токену (ротация); повторное использование отозванного отзывает все сессии
    /// </summary>
    /// <param name="request">Refresh-токен</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestApiModel request, CancellationToken cancellationToken)
    {
        var result = await authService.Refresh(mapper.Map<RefreshRequestModel>(request), cancellationToken);
        return Ok(mapper.Map<AuthResponseApiModel>(result));
    }

    /// <summary>
    /// Выход: отзыв активного refresh-токена
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.Logout(GetUserId(), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Профиль текущего пользователя: идентификатор, логин, имя, роль
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await authService.GetProfile(GetUserId(), cancellationToken);
        return Ok(mapper.Map<UserProfileApiModel>(profile));
    }

    /// <summary>
    /// Смена пароля (обязательна при временном пароле); меняет метку безопасности и завершает все сессии
    /// </summary>
    /// <param name="request">Текущий и новый пароль</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [Authorize]
    [HttpPost("change-password")]
    [EnableRateLimiting(ChangePasswordRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestApiModel request, CancellationToken cancellationToken)
    {
        await authService.ChangePassword(GetUserId(), mapper.Map<ChangePasswordModel>(request), cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("В токене отсутствует клейм идентификатора пользователя"));
}