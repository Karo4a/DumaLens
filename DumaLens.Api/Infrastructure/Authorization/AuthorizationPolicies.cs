namespace DumaLens.Api.Infrastructure.Authorization;

/// <summary>
/// Имена политик авторизации Api
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Минимальная роль Moderator (проходят Moderator и Administrator)
    /// </summary>
    public const string Moderator = "Moderator";

    /// <summary>
    /// Минимальная роль Administrator
    /// </summary>
    public const string Administrator = "Administrator";
}