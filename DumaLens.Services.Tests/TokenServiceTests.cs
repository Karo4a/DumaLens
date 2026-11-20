using DumaLens.Entities.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Services.Auth;
using DumaLens.Services.Contracts;
using DumaLens.Services.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Тесты выпуска access-токена (ADR-13): клеймы, подпись HMAC-SHA256, срок жизни
/// </summary>
public class TokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly AuthOptions options = new()
    {
        SigningKey = AuthTestBase.TestSigningKey,
        Issuer = "DumaLens.Api",
        Audience = "DumaLens.Web",
        AccessTokenLifetimeMinutes = 15,
    };

    private readonly UserDbModel user = new()
    {
        Id = Guid.NewGuid(),
        Login = "admin",
        Name = "Администратор",
        Role = Role.Administrator,
        SecurityStamp = Guid.NewGuid(),
    };

    /// <summary>
    /// Токен содержит клеймы sub, name, role, security_stamp, jti и проходит проверку подписи
    /// </summary>
    [Fact]
    public async Task CreateAccessTokenShouldContainClaimsAndValidSignature()
    {
        ITokenService service = new TokenService(options, new TestDateTimeProvider(Now));
        var token = service.CreateAccessToken(user);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            // Срок жизни проверяется относительно тестовых часов, а не системных
            LifetimeValidator = (notBefore, expires, securityToken, validationParameters) =>
                Now.UtcDateTime >= (notBefore ?? DateTime.MinValue) && Now.UtcDateTime < (expires ?? DateTime.MaxValue),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(options.SigningKey)),
        });
        result.IsValid.Should().BeTrue();
        var jwt = handler.ReadJsonWebToken(token);
        jwt.GetClaim("sub").Value.Should().Be(user.Id.ToString());
        jwt.GetClaim("name").Value.Should().Be(user.Login);
        jwt.GetClaim("role").Value.Should().Be("Administrator");
        jwt.GetClaim("security_stamp").Value.Should().Be(user.SecurityStamp.ToString());
        jwt.GetClaim("pwd_temp").Value.Should().Be("false");
        jwt.GetClaim("jti").Value.Should().NotBeNullOrEmpty();
        jwt.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Срок жизни токена — заданные минуты от момента выпуска
    /// </summary>
    [Fact]
    public void CreateAccessTokenShouldExpireAfterConfiguredLifetime()
    {
        ITokenService service = new TokenService(options, new TestDateTimeProvider(Now));
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(service.CreateAccessToken(user));
        jwt.ValidTo.Should().Be(Now.UtcDateTime.AddMinutes(15));
    }

    /// <summary>
    /// Токен, подписанный другим ключом, не проходит проверку
    /// </summary>
    [Fact]
    public async Task TokenWithWrongKeyShouldFailValidation()
    {
        var foreignOptions = new AuthOptions
        {
            SigningKey = Convert.ToBase64String(Enumerable.Repeat((byte)9, 32).ToArray()),
            Issuer = options.Issuer,
            Audience = options.Audience,
        };
        ITokenService service = new TokenService(foreignOptions, new TestDateTimeProvider(Now));
        var token = service.CreateAccessToken(user);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(options.SigningKey)),
        });
        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// Без ключа подписи выпуск токена невозможен
    /// </summary>
    [Fact]
    public void CreateAccessTokenWithoutSigningKeyShouldThrow()
    {
        ITokenService service = new TokenService(new AuthOptions(), new TestDateTimeProvider(Now));
        var act = () => service.CreateAccessToken(user);
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Временный пароль отражается в клейме pwd_temp (принуждение смены на уровне авторизации Api)
    /// </summary>
    [Fact]
    public void CreateAccessTokenShouldCarryTemporaryPasswordFlag()
    {
        var temporaryUser = new UserDbModel
        {
            Id = Guid.NewGuid(),
            Login = "fresh",
            Role = Role.User,
            SecurityStamp = Guid.NewGuid(),
            PasswordIsTemporary = true,
        };
        ITokenService service = new TokenService(options, new TestDateTimeProvider(Now));
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(service.CreateAccessToken(temporaryUser));
        jwt.GetClaim("pwd_temp").Value.Should().Be("true");
    }
}