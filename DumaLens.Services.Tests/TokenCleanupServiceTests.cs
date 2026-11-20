using DumaLens.Entities;
using DumaLens.Services.Auth;
using DumaLens.Services.Contracts;
using DumaLens.Services.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Очистка утративших силу refresh-токенов на одноразовой PostgreSQL-базе:
/// удаляются только давно истёкшие/отозванные, активные и недавно отозванные сохраняются
/// </summary>
public sealed class TokenCleanupServiceTests : AuthTestBase
{
    /// <summary>
    /// Очистка удаляет давно истёкшие и давно отозванные токены, не трогая активные и недавние
    /// </summary>
    [SkippableFact]
    public async Task CleanupShouldRemoveOnlyOldInactiveTokens()
    {
        SkipIfUnavailable();
        var activeUser = await SeedUser("active");
        var recentUser = await SeedUser("recent");
        var expiredUser = await SeedUser("expired");
        var revokedUser = await SeedUser("revoked");
        var retention = Options.RefreshTokenRetentionDays;

        var active = NewToken(activeUser.Id, expiresAt: StartTime.AddDays(Options.RefreshTokenLifetimeDays), revokedAt: null);
        var recentlyRevoked = NewToken(recentUser.Id, expiresAt: StartTime.AddDays(Options.RefreshTokenLifetimeDays), revokedAt: StartTime.AddDays(-1));
        var oldExpired = NewToken(expiredUser.Id, expiresAt: StartTime.AddDays(-retention - 1), revokedAt: null);
        var oldRevoked = NewToken(revokedUser.Id, expiresAt: StartTime.AddDays(-retention - 5), revokedAt: StartTime.AddDays(-retention - 1));
        Context.Set<RefreshToken>().AddRange(active, recentlyRevoked, oldExpired, oldRevoked);
        await Context.SaveChangesAsync();

        ITokenCleanupService service = new TokenCleanupService(RefreshWrite, Clock, Options);
        var deleted = await service.CleanupAsync(CancellationToken.None);

        deleted.Should().Be(2);
        var remaining = await Context.Set<RefreshToken>().AsNoTracking().Select(x => x.Id).ToListAsync();
        remaining.Should().BeEquivalentTo(new[] { active.Id, recentlyRevoked.Id });
    }

    /// <summary>
    /// Повторная очистка без новых данных ничего не удаляет
    /// </summary>
    [SkippableFact]
    public async Task CleanupShouldBeIdempotent()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var old = NewToken(user.Id, expiresAt: StartTime.AddDays(-Options.RefreshTokenRetentionDays - 1), revokedAt: null);
        Context.Set<RefreshToken>().Add(old);
        await Context.SaveChangesAsync();

        ITokenCleanupService service = new TokenCleanupService(RefreshWrite, Clock, Options);
        (await service.CleanupAsync(CancellationToken.None)).Should().Be(1);
        (await service.CleanupAsync(CancellationToken.None)).Should().Be(0);
    }

    private static RefreshToken NewToken(Guid userId, DateTimeOffset expiresAt, DateTimeOffset? revokedAt)
        => new()
        {
            UserId = userId,
            TokenHash = SecurityHelper.HashRefreshToken(SecurityHelper.GenerateRefreshToken()),
            SecurityStamp = Guid.NewGuid(),
            CreatedAt = StartTime.AddDays(-40),
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
        };
}