using DumaLens.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DumaLens.Api.Infrastructure.Authorization;

/// <summary>
/// Ограничивает доступ к не-auth эндпоинтам, пока пароль пользователя временный (принуждение смены на уровне Api).
/// Проверяется только там, где есть [Authorize]: анонимные эндпоинты ([AllowAnonymous]) авторизацию не проходят
/// и не затрагиваются. Эндпоинты аутентификации помечены <see cref="AllowTemporaryPasswordAttribute"/>.
/// Признак читается из клейма токена — обращения к базе на каждый запрос нет.
/// </summary>
public class TemporaryPasswordHandler : AuthorizationHandler<TemporaryPasswordRequirement>
{
    /// <inheritdoc cref="AuthorizationHandler{TRequirement}.HandleRequirementAsync"/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TemporaryPasswordRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext
            && httpContext.GetEndpoint()?.Metadata.GetMetadata<AllowTemporaryPasswordAttribute>() != null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        if (context.User.FindFirstValue(TokenService.TemporaryPasswordClaimType) == "true")
        {
            context.Fail();
            return Task.CompletedTask;
        }
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}