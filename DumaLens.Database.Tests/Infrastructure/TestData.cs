using Ahatornn.TestGenerator;
using DumaLens.Entities;
using DumaLens.Entities.Contracts;
using DumaLens.Entities.Default;
using DumaLens.Entities.Enums;
using System.Security.Cryptography;
using System.Text;

namespace DumaLens.Database.Tests.Infrastructure;

/// <summary>
/// Фабрики тестовых сущностей для тестов базы данных
/// </summary>
internal static class TestData
{
    /// <summary>
    /// Задаёт детерминированный UTC-аудит и отсутствие мягкого удаления
    /// </summary>
    private static void SetAudit(DbBaseEntity entity)
    {
        entity.CreatedAt = SeedTimestamp.Value;
        entity.UpdatedAt = SeedTimestamp.Value;
        entity.DeletedAt = null;
    }

    /// <summary>
    /// Создаёт созыв с заданным номером и кодом сайта
    /// </summary>
    internal static Convocation CreateConvocation(int number, string siteCode)
        => TestEntityProvider.Shared.Create<Convocation>(x =>
        {
            SetAudit(x);
            x.Number = number;
            x.SiteCode = siteCode;
            x.Title = $"Тестовый созыв {number}";
            x.DateFrom = new DateOnly(2020, 1, 1);
            x.DateTo = null;
        });

    /// <summary>
    /// Создаёт каноническую фракцию с заданным кодом
    /// </summary>
    internal static Faction CreateFaction(string code)
        => TestEntityProvider.Shared.Create<Faction>(x =>
        {
            SetAudit(x);
            x.Code = code;
            x.Title = "Тестовая фракция";
        });

    /// <summary>
    /// Создаёт депутата с заданным идентификатором сайта
    /// </summary>
    internal static Deputy CreateDeputy(long siteId)
        => TestEntityProvider.Shared.Create<Deputy>(x =>
        {
            SetAudit(x);
            x.SiteId = siteId;
            x.FullName = "Тестовый Депутат Тестович";
            x.SortName = "Тестовый Т.Т.";
        });

    /// <summary>
    /// Создаёт законопроект с заданным номером
    /// </summary>
    internal static Bill CreateBill(string number)
        => TestEntityProvider.Shared.Create<Bill>(x =>
        {
            SetAudit(x);
            x.Number = number;
        });

    /// <summary>
    /// Создаёт голосование в заданном созыве с заданным идентификатором сайта
    /// </summary>
    internal static Vote CreateVote(Guid convocationId, long siteId)
        => TestEntityProvider.Shared.Create<Vote>(x =>
        {
            SetAudit(x);
            x.ConvocationId = convocationId;
            x.SiteId = siteId;
            x.BillId = null;
            x.Title = "О проекте федерального закона «О тестовом законе»";
            x.VotedAt = new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(4));
            x.UtcOffsetMinutes = 240;
            x.Stage = VoteStage.SecondReading;
            x.StageRaw = "2 чтение";
            x.Outcome = VoteOutcome.Accepted;
            x.AcceptPercentSite = null;
            x.HasResults = true;
            x.Source = VoteSource.Archive;
            x.Convocation = null!;
            x.Bill = null;
        });

    /// <summary>
    /// Создаёт пользователя с заданными логином и почтой
    /// </summary>
    internal static User CreateUser(string login, string email)
        => TestEntityProvider.Shared.Create<User>(x =>
        {
            SetAudit(x);
            x.Login = login;
            x.LoginLowerCase = login.ToLowerInvariant();
            x.Email = email;
            x.EmailLowerCase = email.ToLowerInvariant();
            x.Name = "Тестовый пользователь";
            x.PasswordHash = Convert.ToBase64String(new byte[32]);
            x.PasswordSalt = Convert.ToBase64String(new byte[16]);
            x.RoleId = UserRolesDefault.DefaultRoleToGuid(Role.User);
            x.Note = null;
            x.Role = null!;
        });

    /// <summary>
    /// Создаёт активный refresh-токен пользователя с заданным хэшем
    /// </summary>
    internal static RefreshToken CreateRefreshToken(Guid userId, string tokenHash)
        => TestEntityProvider.Shared.Create<RefreshToken>(x =>
        {
            x.UserId = userId;
            x.TokenHash = tokenHash;
            x.ExpiresAt = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
            x.CreatedAt = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
            x.RevokedAt = null;
        });

    /// <summary>
    /// Вычисляет base64-хэш SHA-256 строки (32 байта, 44 символа base64)
    /// </summary>
    internal static string HashOf(string value)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}