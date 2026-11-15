using DumaLens.Context.Tests;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Services.Stats;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Персистентность фракционных агрегатов (Э4): строки VoteFactionStats сохраняются
/// с составным ключом (VoteId, FactionPeriodId) и переживают чтение из контекста
/// </summary>
public sealed class VoteFactionStatsPersistenceTests : DumaLensContextInMemory
{
    /// <summary>
    /// Сохранённые агрегаты читаются по составному ключу, значения процентов не теряют точность
    /// </summary>
    [Fact]
    public async Task SavedFactionStatsShouldBeReadableByCompositeKey()
    {
        var period = await PrepareFactionPeriod();
        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            SiteId = 126282,
            Title = "О проекте федерального закона",
            VotedAt = new DateTimeOffset(2024, 3, 13, 11, 54, 12, TimeSpan.FromHours(3)),
            ConvocationId = period.ConvocationId,
            Stage = VoteStage.Unknown,
            Outcome = VoteOutcome.Unknown,
            Source = VoteSource.Archive,
            HasResults = true,
        };
        Context.Set<Vote>().Add(vote);
        var deputies = new List<Deputy>();
        for (var i = 0; i < 3; i++)
        {
            deputies.Add(await PrepareDeputy());
        }

        await Context.SaveChangesAsync();

        var deputyVotes = new List<DeputyVote>
        {
            new() { VoteId = vote.Id, DeputyId = deputies[0].Id, FactionPeriodId = period.Id, Result = VoteResult.Accept },
            new() { VoteId = vote.Id, DeputyId = deputies[1].Id, FactionPeriodId = period.Id, Result = VoteResult.Accept },
            new() { VoteId = vote.Id, DeputyId = deputies[2].Id, FactionPeriodId = period.Id, Result = VoteResult.None },
        };
        var service = new VoteFactionStatsService();
        var stats = service.BuildStats(vote.Id, deputyVotes, new Dictionary<Guid, Guid> { [period.Id] = period.FactionId });
        Context.Set<VoteFactionStats>().AddRange(stats);
        await Context.SaveChangesAsync();

        var saved = await Context.Set<VoteFactionStats>().AsNoTracking()
            .SingleAsync(x => x.VoteId == vote.Id && x.FactionPeriodId == period.Id);
        saved.FactionId.Should().Be(period.FactionId);
        saved.AcceptCount.Should().Be(2);
        saved.NoneCount.Should().Be(1);
        saved.MemberCount.Should().Be(3);
        saved.VotedCount.Should().Be(2);
        saved.AcceptPctMembers.Should().Be(66.67m);
        saved.NonePctMembers.Should().Be(33.33m);
        saved.AcceptPctVoted.Should().Be(100.00m);
    }
}