using DumaLens.Common.Contracts;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Services.Contracts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace DumaLens.Services.Auth;

/// <inheritdoc cref="ITokenService"/>
/// <remarks>
/// JWT JWS с подписью HMAC-SHA256, без шифрования (ADR-13): Web читает клеймы.
/// Клеймы: sub (UserId), name (логин), role, security_stamp, jti.
/// </remarks>
public class TokenService : ITokenService, IServiceAnchor
{
    /// <summary>
    /// Клейм признака временного пароля: авторизация Api ограничивает доступ к не-auth эндпоинтам,
    /// пока пароль не сменён (принуждение смены на уровне Api, а не только Web UI)
    /// </summary>
    public const string TemporaryPasswordClaimType = "pwd_temp";

    private readonly AuthOptions options;
    private readonly IDateTimeProvider dateTimeProvider;

    /// <summary>
    /// Конструктор
    /// </summary>
    public TokenService(AuthOptions options, IDateTimeProvider dateTimeProvider)
    {
        this.options = options;
        this.dateTimeProvider = dateTimeProvider;
    }

    string ITokenService.CreateAccessToken(UserDbModel user)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException("Ключ подписи JWT не настроен: задайте Auth:SigningKey (base64, не менее 32 байт)");
        }
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Login),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", user.Role.ToString()),
            new("security_stamp", user.SecurityStamp.ToString()),
            new(TemporaryPasswordClaimType, user.PasswordIsTemporary ? "true" : "false"),
        };
        var signKey = new SymmetricSecurityKey(Convert.FromBase64String(options.SigningKey));
        var now = dateTimeProvider.UtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = now.AddMinutes(options.AccessTokenLifetimeMinutes),
            SigningCredentials = new SigningCredentials(signKey, SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}