using DumaLens.Context;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DumaLens.Importer.Stats;

/// <summary>
/// Очистка ведущих служебных маркеров сайта «*»/«**» в заголовках существующих голосований.
/// Set-based UPDATE (идемпотентно), зеркалит LeadingMarkersRegex нормализатора заголовков:
/// новые импорты уже пишут очищенный заголовок, команда приводит к нему накопленные данные.
/// </summary>
public sealed class TitleRenormalizer
{
    private const string UpdateTitlesSql =
        "UPDATE \"Votes\" SET \"Title\" = regexp_replace(\"Title\", '^[\\s*]+', '') WHERE \"Title\" ~ '^[\\s*]+'";

    private readonly DumaLensContext context;

    /// <summary>
    /// Конструктор
    /// </summary>
    public TitleRenormalizer(DumaLensContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Снять ведущие маркеры со всех заголовков, где они есть; повторный запуск ничего не меняет
    /// </summary>
    public async Task<TitleRenormalizeSummary> Run(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var affected = await context.Database.ExecuteSqlRawAsync(UpdateTitlesSql, cancellationToken);
        stopwatch.Stop();
        return new TitleRenormalizeSummary(affected, stopwatch.Elapsed);
    }
}

/// <summary>
/// Итог очистки заголовков
/// </summary>
/// <param name="TitlesUpdated">Число обновлённых заголовков</param>
/// <param name="Elapsed">Затраченное время</param>
public sealed record TitleRenormalizeSummary(int TitlesUpdated, TimeSpan Elapsed);