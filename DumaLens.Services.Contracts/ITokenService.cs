using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис выпуска токенов (ADR-13: JWT JWS HMAC-SHA256, без шифрования)
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Создаёт подписанный access-токен с клеймами sub, name, role, security_stamp, jti
    /// </summary>
    string CreateAccessToken(UserDbModel user);
}