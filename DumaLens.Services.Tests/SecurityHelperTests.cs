using DumaLens.Services.Auth;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Тесты криптографии паролей и refresh-токенов (ADR-12, ADR-13; 07-security.md §5)
/// </summary>
public class SecurityHelperTests
{
    /// <summary>
    /// Число итераций по умолчанию — не менее 100 000 (ADR-12; в эталонах 6 — не копируем)
    /// </summary>
    [Fact]
    public void DefaultIterationsShouldBeAtLeast100000()
        => SecurityHelper.DefaultIterations.Should().BeGreaterThanOrEqualTo(100_000);

    /// <summary>
    /// Хэш детерминирован при одинаковых пароле и соли
    /// </summary>
    [Fact]
    public void HashPasswordShouldBeDeterministicWithSameSalt()
    {
        var salt = SecurityHelper.GenerateSalt();
        var first = SecurityHelper.HashPassword("Password123", salt);
        var second = SecurityHelper.HashPassword("Password123", salt);
        first.Should().Be(second);
    }

    /// <summary>
    /// Разные соли дают разные хэши одного пароля
    /// </summary>
    [Fact]
    public void HashPasswordShouldDifferWithDifferentSalts()
    {
        var first = SecurityHelper.HashPassword("Password123", SecurityHelper.GenerateSalt());
        var second = SecurityHelper.HashPassword("Password123", SecurityHelper.GenerateSalt());
        first.Should().NotBe(second);
    }

    /// <summary>
    /// Хэш совпадает с независимым вычислением PBKDF2-SHA256 (100 000 итераций, 32 байта)
    /// </summary>
    [Fact]
    public void HashPasswordShouldMatchIndependentPbkdf2Computation()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(32);
        var salt = Convert.ToBase64String(saltBytes);
        var expected = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2("Password123", saltBytes, 100_000, HashAlgorithmName.SHA256, 32));
        SecurityHelper.HashPassword("Password123", salt).Should().Be(expected);
    }

    /// <summary>
    /// Соль и хэш — 32 байта (base64)
    /// </summary>
    [Fact]
    public void SaltAndHashShouldBe32Bytes()
    {
        Convert.FromBase64String(SecurityHelper.GenerateSalt()).Should().HaveCount(32);
        Convert.FromBase64String(SecurityHelper.HashPassword("Password123", SecurityHelper.GenerateSalt())).Should().HaveCount(32);
    }

    /// <summary>
    /// Проверка пароля: верный пароль проходит, неверный и повреждённый хэш — нет
    /// </summary>
    [Fact]
    public void VerifyPasswordShouldReturnTrueOnlyForCorrectPassword()
    {
        var salt = SecurityHelper.GenerateSalt();
        var hash = SecurityHelper.HashPassword("Password123", salt);
        SecurityHelper.VerifyPassword("Password123", salt, hash).Should().BeTrue();
        SecurityHelper.VerifyPassword("password123", salt, hash).Should().BeFalse();
        SecurityHelper.VerifyPassword("Password123", salt, Convert.ToBase64String(new byte[16])).Should().BeFalse();
        SecurityHelper.VerifyPassword("Password123", salt, "не-base64").Should().BeFalse();
    }

    /// <summary>
    /// Refresh-токен — 32 случайных байта, значения уникальны
    /// </summary>
    [Fact]
    public void GenerateRefreshTokenShouldBe32RandomBytes()
    {
        var first = SecurityHelper.GenerateRefreshToken();
        var second = SecurityHelper.GenerateRefreshToken();
        Convert.FromBase64String(first).Should().HaveCount(32);
        first.Should().NotBe(second);
    }

    /// <summary>
    /// Хэш refresh-токена — SHA-256 от UTF-8 представления токена
    /// </summary>
    [Fact]
    public void HashRefreshTokenShouldBeSha256OfToken()
    {
        var token = SecurityHelper.GenerateRefreshToken();
        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        SecurityHelper.HashRefreshToken(token).Should().Be(expected);
    }

    /// <summary>
    /// Временный пароль: заданная длина, допустимый алфавит, уникальность; длина меньше 8 запрещена
    /// </summary>
    [Fact]
    public void GenerateTemporaryPasswordShouldMeetLengthAndAlphabet()
    {
        var first = SecurityHelper.GenerateTemporaryPassword();
        var second = SecurityHelper.GenerateTemporaryPassword();
        first.Should().HaveLength(16);
        first.Should().NotBe(second);
        first.Should().MatchRegex("^[abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789]+$");
        SecurityHelper.GenerateTemporaryPassword(8).Should().HaveLength(8);
        var act = () => SecurityHelper.GenerateTemporaryPassword(7);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}