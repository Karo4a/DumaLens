using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Contracts.Models;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DumaLens.Parsing;

/// <summary>
/// Парсер JSON-файла голосования архива vote-master.zip
/// </summary>
/// <remarks>
/// Формат: id, title, datetime (ISO со смещением +0300/+0400), votes (deputy.id/name/faction, result).
/// Пустое голосование — votes: [] (HasResults = false). Итога в архиве нет — Outcome не заполняется.
/// </remarks>
public sealed class VoteArchiveParser : IVoteArchiveParser
{
    /// <inheritdoc/>
    public ParsedVote Parse(string json)
    {
        var vote = new ParsedVote { Source = VoteSource.Archive };
        var issues = new List<ParseIssue>();

        ArchiveVote? archiveVote;
        try
        {
            archiveVote = JsonSerializer.Deserialize<ArchiveVote>(json);
        }
        catch (JsonException ex)
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.InvalidJson,
                Message = $"Некорректный JSON файла архива: {ex.Message}"
            });
            vote.Issues = issues;
            return vote;
        }

        if (archiveVote is null)
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.InvalidJson,
                Message = "Файл архива не содержит объекта голосования"
            });
            vote.Issues = issues;
            return vote;
        }

        vote.SiteId = long.TryParse(archiveVote.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var siteId)
            ? siteId
            : 0;
        vote.TitleRaw = archiveVote.Title ?? string.Empty;
        vote.Title = HtmlText.Clean(archiveVote.Title);

        if (DateTimeOffset.TryParse(
                archiveVote.Datetime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var votedAt))
        {
            vote.VotedAt = votedAt;
        }

        var deputyVotes = new List<ParsedDeputyVote>();
        if (archiveVote.Votes is not null)
        {
            foreach (var archiveDeputyVote in archiveVote.Votes)
            {
                var deputy = archiveDeputyVote.Deputy;
                deputyVotes.Add(new ParsedDeputyVote
                {
                    DeputySiteId = long.TryParse(deputy?.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var deputyId)
                        ? deputyId
                        : 0,
                    FullName = HtmlText.CleanFullName(deputy?.Name),
                    FactionSiteId = long.TryParse(deputy?.Faction?.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var factionId)
                        ? factionId
                        : null,
                    FactionTitle = string.IsNullOrWhiteSpace(deputy?.Faction?.Title) ? null : deputy!.Faction!.Title!.Trim(),
                    ResultRaw = archiveDeputyVote.Result ?? string.Empty,
                    Result = MapResult(archiveDeputyVote.Result)
                });
            }
        }

        vote.DeputyVotes = deputyVotes;
        vote.Issues = issues;
        return vote;
    }

    /// <summary>
    /// Маппинг кодов архива: accept/decline/abstain/none → канонический результат
    /// </summary>
    /// <remarks>
    /// Сравнение без приведения регистра и аллокаций: коды архива строчные, проверка OrdinalIgnoreCase
    /// сохраняет совместимость с произвольным регистром.
    /// </remarks>
    private static VoteResult? MapResult(string? code)
    {
        if (code is null)
        {
            return null;
        }

        if (code.Equals("accept", StringComparison.OrdinalIgnoreCase))
        {
            return VoteResult.Accept;
        }

        if (code.Equals("decline", StringComparison.OrdinalIgnoreCase))
        {
            return VoteResult.Decline;
        }

        if (code.Equals("abstain", StringComparison.OrdinalIgnoreCase))
        {
            return VoteResult.Abstain;
        }

        if (code.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return VoteResult.None;
        }

        return null;
    }

    /// <summary>
    /// DTO файла архива
    /// </summary>
    private sealed class ArchiveVote
    {
        /// <summary>
        /// Идентификатор голосования на сайте (строкой)
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Заголовок голосования
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Дата и время ISO с историческим смещением Москвы
        /// </summary>
        [JsonPropertyName("datetime")]
        public string? Datetime { get; set; }

        /// <summary>
        /// Индивидуальные голоса (пустой массив для пустых голосований)
        /// </summary>
        [JsonPropertyName("votes")]
        public List<ArchiveDeputyVote>? Votes { get; set; }
    }

    /// <summary>
    /// DTO голоса депутата в файле архива
    /// </summary>
    private sealed class ArchiveDeputyVote
    {
        /// <summary>
        /// Депутат (вложенный объект)
        /// </summary>
        [JsonPropertyName("deputy")]
        public ArchiveDeputy? Deputy { get; set; }

        /// <summary>
        /// Код результата (accept/decline/abstain/none)
        /// </summary>
        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }

    /// <summary>
    /// DTO депутата в файле архива
    /// </summary>
    private sealed class ArchiveDeputy
    {
        /// <summary>
        /// Идентификатор депутата на сайте (строкой)
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// ФИО депутата
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Фракция депутата
        /// </summary>
        [JsonPropertyName("faction")]
        public ArchiveFaction? Faction { get; set; }
    }

    /// <summary>
    /// DTO фракции в файле архива
    /// </summary>
    private sealed class ArchiveFaction
    {
        /// <summary>
        /// Идентификатор фракции на сайте (строкой)
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Короткое название фракции
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}