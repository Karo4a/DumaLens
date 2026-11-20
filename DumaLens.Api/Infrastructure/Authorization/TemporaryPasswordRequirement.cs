using Microsoft.AspNetCore.Authorization;

namespace DumaLens.Api.Infrastructure.Authorization;

/// <summary>
/// Требование: пароль пользователя не временный либо эндпоинт освобождён атрибутом <see cref="AllowTemporaryPasswordAttribute"/>
/// </summary>
public class TemporaryPasswordRequirement : IAuthorizationRequirement
{
}