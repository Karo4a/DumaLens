using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Importer.Verify;

/// <summary>
/// Сверка контрольных сумм с паспортом архива: итоги, суммы по созывам и кодам результатов.
/// Фактические значения включают записи карантина архива (база + карантин = паспорт):
/// аномальные файлы не попадают в основную базу, но учитываются в сверке по сырому JSON
/// </summary>
public sealed class ArchiveVerifier
{
    private static readonly string[] RomanNumbers = ["I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX"];

    private readonly IReader reader;
    private readonly IVoteArchiveParser parser;
    private readonly ITitleNormalizer normalizer;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ArchiveVerifier(IReader reader, IVoteArchiveParser parser, ITitleNormalizer normalizer)
    {
        this.reader = reader;
        this.parser = parser;
        this.normalizer = normalizer;
    }

    /// <summary>
    /// Выполнить сверку и вернуть отчёт
    /// </summary>
    public async Task<VerifyReport> Verify(CancellationToken cancellationToken)
    {
        var report = new VerifyReport();
        var votes = reader.Read<Vote>().NotDeletedAt();
        var deputyVotes = reader.Read<DeputyVote>();

        var convocations = await reader.Read<Convocation>()
            .NotDeletedAt()
            .OrderBy(x => x.Number)
            .Select(x => new ConvocationRow(x.Id, x.Number, x.DateFrom, x.DateTo))
            .ToListAsync(cancellationToken);

        var dbVotes = await votes.CountAsync(cancellationToken);
        var dbDeputyVotes = await deputyVotes.CountAsync(cancellationToken);
        var votesByConvocation = await votes
            .GroupBy(x => x.ConvocationId)
            .Select(g => new { ConvocationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConvocationId, x => (long)x.Count, cancellationToken);
        var deputyVotesByConvocation = await (
                from deputyVote in deputyVotes
                join vote in votes on deputyVote.VoteId equals vote.Id
                group deputyVote by vote.ConvocationId into g
                select new { ConvocationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConvocationId, x => (long)x.Count, cancellationToken);
        var resultTotals = await deputyVotes
            .GroupBy(x => x.Result)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Result, x => (long)x.Count, cancellationToken);
        var dbEmptyVotes = await votes.CountAsync(x => !x.HasResults, cancellationToken);
        var dbBillNumbers = await reader.Read<Bill>()
            .NotDeletedAt()
            .Select(x => x.Number)
            .ToListAsync(cancellationToken);

        var quarantine = await SumQuarantine(convocations, cancellationToken);

        report.DbVotes = dbVotes;
        report.DbDeputyVotes = dbDeputyVotes;
        report.QuarantineVotes = quarantine.Votes;
        report.QuarantineDeputyVotes = quarantine.DeputyVotes;
        report.QuarantineCount = quarantine.Votes;

        report.Rows.Add(new ChecksumRow
        {
            Name = "Всего голосований",
            Expected = ArchivePassport.TotalVotes,
            Actual = dbVotes + quarantine.Votes,
        });
        report.Rows.Add(new ChecksumRow
        {
            Name = "Всего индивидуальных голосов",
            Expected = ArchivePassport.TotalDeputyVotes,
            Actual = dbDeputyVotes + quarantine.DeputyVotes,
        });

        foreach (var convocation in convocations)
        {
            if (!ArchivePassport.Convocations.TryGetValue(convocation.Number, out var expected))
            {
                continue;
            }

            var roman = RomanNumbers[convocation.Number - 1];
            report.Rows.Add(new ChecksumRow
            {
                Name = $"Созыв {roman}: голосования",
                Expected = expected.Votes,
                Actual = votesByConvocation.GetValueOrDefault(convocation.Id) + quarantine.VotesByConvocation.GetValueOrDefault(convocation.Id),
            });
            report.Rows.Add(new ChecksumRow
            {
                Name = $"Созыв {roman}: индивидуальные голоса",
                Expected = expected.DeputyVotes,
                Actual = deputyVotesByConvocation.GetValueOrDefault(convocation.Id) + quarantine.DeputyVotesByConvocation.GetValueOrDefault(convocation.Id),
            });
        }

        foreach (var (result, expected) in ArchivePassport.ResultTotals)
        {
            report.Rows.Add(new ChecksumRow
            {
                Name = $"{ResultCode(result)} ({ResultName(result)})",
                Expected = expected,
                Actual = resultTotals.GetValueOrDefault(result) + quarantine.ResultTotals.GetValueOrDefault(result),
            });
        }

        report.Rows.Add(new ChecksumRow
        {
            Name = "Пустые голосования (HasResults = false)",
            Expected = ArchivePassport.EmptyVotes,
            Actual = dbEmptyVotes + quarantine.EmptyVotes,
        });

        var billNumbers = dbBillNumbers.ToHashSet(StringComparer.Ordinal);
        foreach (var number in quarantine.BillNumbers)
        {
            billNumbers.Add(number);
        }

        report.Rows.Add(new ChecksumRow
        {
            Name = "Уникальные номера законопроектов",
            Expected = ArchivePassport.UniqueBillNumbers,
            Actual = billNumbers.Count,
        });

        return report;
    }

    private async Task<QuarantineSums> SumQuarantine(
        IReadOnlyList<ConvocationRow> convocations,
        CancellationToken cancellationToken)
    {
        var sums = new QuarantineSums();
        var records = await reader.Read<ImportQuarantine>()
            .NotDeletedAt()
            .Where(x => x.SourceFile != null)
            .Select(x => x.RawJson)
            .ToListAsync(cancellationToken);

        foreach (var rawJson in records)
        {
            var parsed = parser.Parse(rawJson);
            sums.Votes++;
            sums.DeputyVotes += parsed.DeputyVotes.Count;
            if (!parsed.HasResults)
            {
                sums.EmptyVotes++;
            }

            var convocationId = FindConvocationId(convocations, parsed.VotedAt);
            if (convocationId.HasValue)
            {
                sums.VotesByConvocation[convocationId.Value] = sums.VotesByConvocation.GetValueOrDefault(convocationId.Value) + 1;
                sums.DeputyVotesByConvocation[convocationId.Value] = sums.DeputyVotesByConvocation.GetValueOrDefault(convocationId.Value) + parsed.DeputyVotes.Count;
            }

            foreach (var deputyVote in parsed.DeputyVotes)
            {
                if (deputyVote.Result.HasValue)
                {
                    sums.ResultTotals[deputyVote.Result.Value] = sums.ResultTotals.GetValueOrDefault(deputyVote.Result.Value) + 1;
                }
            }

            var billNumber = normalizer.Normalize(parsed.Title).BillNumber;
            if (billNumber != null)
            {
                sums.BillNumbers.Add(billNumber);
            }
        }

        return sums;
    }

    private static Guid? FindConvocationId(IReadOnlyList<ConvocationRow> convocations, DateTimeOffset votedAt)
    {
        if (votedAt == default)
        {
            return null;
        }

        var date = DateOnly.FromDateTime(votedAt.DateTime);
        foreach (var convocation in convocations)
        {
            if (convocation.DateFrom <= date && (convocation.DateTo == null || date <= convocation.DateTo))
            {
                return convocation.Id;
            }
        }

        return null;
    }

    private static string ResultCode(VoteResult result)
    {
        return result switch
        {
            VoteResult.Accept => "accept",
            VoteResult.Decline => "decline",
            VoteResult.Abstain => "abstain",
            VoteResult.None => "none",
            _ => "unknown",
        };
    }

    private static string ResultName(VoteResult result)
    {
        return result switch
        {
            VoteResult.Accept => "за",
            VoteResult.Decline => "против",
            VoteResult.Abstain => "воздержался",
            VoteResult.None => "не голосовал",
            _ => "неизвестно",
        };
    }

    private sealed record ConvocationRow(Guid Id, int Number, DateOnly DateFrom, DateOnly? DateTo);

    private sealed class QuarantineSums
    {
        public long Votes { get; set; }

        public long DeputyVotes { get; set; }

        public long EmptyVotes { get; set; }

        public Dictionary<Guid, long> VotesByConvocation { get; } = [];

        public Dictionary<Guid, long> DeputyVotesByConvocation { get; } = [];

        public Dictionary<VoteResult, long> ResultTotals { get; } = [];

        public HashSet<string> BillNumbers { get; } = new(StringComparer.Ordinal);
    }
}