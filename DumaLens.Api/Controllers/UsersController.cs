using AutoMapper;
using DumaLens.Api.Infrastructure.Authorization;
using DumaLens.Api.Models.Exceptions;
using DumaLens.Api.Models.Users;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DumaLens.Api.Controllers;

/// <summary>
/// Управление пользователями и ролями (только Администратор, 07-security.md §2.4)
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService userService;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UsersController(IUserService userService, IMapper mapper)
    {
        this.userService = userService;
        this.mapper = mapper;
    }

    /// <summary>
    /// Список пользователей с пагинацией
    /// </summary>
    /// <param name="page">Номер страницы (с 1)</param>
    /// <param name="pageSize">Размер страницы (1–100)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet]
    [ProducesResponseType(typeof(UserPageApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetPage([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var query = new UserListQueryModel
        {
            Page = page,
            PageSize = pageSize,
        };
        var result = await userService.GetPage(query, cancellationToken);
        return Ok(mapper.Map<UserPageApiModel>(result));
    }

    /// <summary>
    /// Создание пользователя с указанной ролью и временным паролем
    /// </summary>
    /// <param name="request">Данные нового пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpPost]
    [ProducesResponseType(typeof(UserApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestApiModel request, CancellationToken cancellationToken)
    {
        var result = await userService.Create(mapper.Map<CreateUserModel>(request), cancellationToken);
        return Ok(mapper.Map<UserApiModel>(result));
    }

    /// <summary>
    /// Обновление профиля, роли и блокировки пользователя; смена роли или блокировка завершает сессии
    /// </summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="request">Новые данные пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiValidationExceptionDetail), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequestApiModel request, CancellationToken cancellationToken)
    {
        var result = await userService.Update(id, mapper.Map<UpdateUserModel>(request), cancellationToken);
        return Ok(mapper.Map<UserApiModel>(result));
    }

    /// <summary>
    /// Сброс пароля: генерирует временный пароль и завершает все сессии пользователя
    /// </summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponseApiModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiExceptionDetail), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var result = await userService.ResetPassword(id, cancellationToken);
        return Ok(mapper.Map<ResetPasswordResponseApiModel>(result));
    }
}