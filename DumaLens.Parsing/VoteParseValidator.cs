using DumaLens.Entities.ValidationConstants;
using DumaLens.Parsing.Contracts;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Contracts.Models;

namespace DumaLens.Parsing;

/// <summary>
/// Валидатор результата разбора: замечания парсера плюс семантические проверки (причины карантина)
/// </summary>
/// <remarks>
/// Проверяются: непустой заголовок, корректность id и даты, известные коды результатов,
/// идентификаторы депутатов, число депутатов (0 или 430–460), границы созыва.
/// Границы созыва передаются вызывающим (Э3) из справочника Convocations.
/// </remarks>
public sealed class VoteParseValidator : IVoteParseValidator
{
    /// <inheritdoc/>
    public IReadOnlyList<ParseIssue> Validate(
        ParsedVote vote,
        DateOnly? convocationDateFrom = null,
        DateOnly? convocationDateTo = null)
    {
        var issues = new List<ParseIssue>(vote.Issues);

        if (string.IsNullOrWhiteSpace(vote.Title))
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.EmptyTitle,
                Message = "Заголовок голосования пуст"
            });
        }

        if (vote.SiteId <= 0)
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.InvalidVoteId,
                Message = $"Некорректный идентификатор голосования: {vote.SiteId}"
            });
        }

        if (vote.VotedAt == default)
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.InvalidDate,
                Message = "Дата голосования отсутствует или не распознана"
            });
        }

        for (var i = 0; i < vote.DeputyVotes.Count; i++)
        {
            var deputyVote = vote.DeputyVotes[i];
            if (deputyVote.DeputySiteId <= 0)
            {
                issues.Add(new ParseIssue
                {
                    Code = ParseIssueCode.MissingDeputyId,
                    Message = $"Не извлечён идентификатор депутата (строка {i + 1}, ФИО «{deputyVote.FullName}»)"
                });
            }

            if (deputyVote.Result is null)
            {
                issues.Add(new ParseIssue
                {
                    Code = ParseIssueCode.UnknownResultCode,
                    Message = $"Неизвестный код результата «{deputyVote.ResultRaw}» (депутат {deputyVote.DeputySiteId})"
                });
            }
        }

        var count = vote.DeputyVotes.Count;
        if (count != 0 && (count < VoteValidationConstants.DeputyCountMin || count > VoteValidationConstants.DeputyCountMax))
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.DeputyCountOutOfRange,
                Message = $"Число депутатов {count} вне диапазона {VoteValidationConstants.DeputyCountMin}–{VoteValidationConstants.DeputyCountMax}"
            });
        }

        if (vote.VotedAt != default && (convocationDateFrom.HasValue || convocationDateTo.HasValue))
        {
            var date = DateOnly.FromDateTime(vote.VotedAt.DateTime);
            if ((convocationDateFrom.HasValue && date < convocationDateFrom.Value)
                || (convocationDateTo.HasValue && date > convocationDateTo.Value))
            {
                issues.Add(new ParseIssue
                {
                    Code = ParseIssueCode.DateOutOfConvocationRange,
                    Message = $"Дата {date:dd.MM.yyyy} вне границ созыва {convocationDateFrom:dd.MM.yyyy}–{convocationDateTo:dd.MM.yyyy}"
                });
            }
        }

        return issues;
    }
}