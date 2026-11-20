namespace DumaLens.Api.Infrastructure.Authorization;

/// <summary>
/// Освобождает эндпоинт от ограничения временного пароля (эндпоинты аутентификации:
/// смена пароля, профиль, выход должны работать до смены временного пароля)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowTemporaryPasswordAttribute : Attribute
{
}