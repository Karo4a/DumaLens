using DumaLens.Services.Contracts;
using FluentAssertions;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Валидация настроек аутентификации (fail-fast при старте Api): ключ подписи и диапазоны значений
/// </summary>
public class AuthOptionsTests
{
    /// <summary>
    /// Значения по умолчанию корректны (пустой ключ допустим — временный ключ в Development)
    /// </summary>
    [Fact]
    public void ValidateShouldPassForDefaults()
    {
        var act = () => new AuthOptions().Validate();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Ключ из 32 случайных байт (base64) проходит проверку
    /// </summary>
    [Fact]
    public void ValidateShouldAccept32ByteKey()
    {
        var options = new AuthOptions { SigningKey = Convert.ToBase64String(new byte[32]) };
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Короткий ключ (менее 32 байт) отклоняется при старте, а не при первом логине
    /// </summary>
    [Fact]
    public void ValidateShouldRejectShortKey()
    {
        var options = new AuthOptions { SigningKey = Convert.ToBase64String(new byte[16]) };
        var act = () => options.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*32*");
    }

    /// <summary>
    /// Некорректный base64 отклоняется с понятной ошибкой
    /// </summary>
    [Fact]
    public void ValidateShouldRejectMalformedBase64Key()
    {
        var options = new AuthOptions { SigningKey = "not!base64" };
        var act = () => options.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*base64*");
    }

    /// <summary>
    /// Некорректные диапазоны отклоняются: сроки, итерации (ADR-12), блокировка, rate limit, очистка
    /// </summary>
    [Theory]
    [InlineData(nameof(AuthOptions.AccessTokenLifetimeMinutes), 0)]
    [InlineData(nameof(AuthOptions.RefreshTokenLifetimeDays), 0)]
    [InlineData(nameof(AuthOptions.PasswordIterations), 99_999)]
    [InlineData(nameof(AuthOptions.MaxFailedLoginAttempts), 0)]
    [InlineData(nameof(AuthOptions.LoginLockoutMinutes), 0)]
    [InlineData(nameof(AuthOptions.LoginRateLimitPermitLimit), 0)]
    [InlineData(nameof(AuthOptions.LoginRateLimitWindowSeconds), 0)]
    [InlineData(nameof(AuthOptions.RefreshReuseGraceSeconds), -1)]
    [InlineData(nameof(AuthOptions.RefreshTokenRetentionDays), 0)]
    [InlineData(nameof(AuthOptions.TokenCleanupIntervalHours), 0)]
    public void ValidateShouldRejectInvalidRanges(string propertyName, int value)
    {
        var options = new AuthOptions();
        typeof(AuthOptions).GetProperty(propertyName)!.SetValue(options, value);
        var act = () => options.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{propertyName}*");
    }
}