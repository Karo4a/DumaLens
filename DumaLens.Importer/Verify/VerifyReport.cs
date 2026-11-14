using System.Text;

namespace DumaLens.Importer.Verify;

/// <summary>
/// Отчёт сверки контрольных сумм базы с паспортом архива
/// </summary>
public sealed class VerifyReport
{
    /// <summary>
    /// Строки сверки
    /// </summary>
    public List<ChecksumRow> Rows { get; } = [];

    /// <summary>
    /// Число записей в карантине импорта
    /// </summary>
    public long QuarantineCount { get; set; }

    /// <summary>
    /// Голосований в основной базе
    /// </summary>
    public long DbVotes { get; set; }

    /// <summary>
    /// Индивидуальных голосов в основной базе
    /// </summary>
    public long DbDeputyVotes { get; set; }

    /// <summary>
    /// Голосований в карантине архива
    /// </summary>
    public long QuarantineVotes { get; set; }

    /// <summary>
    /// Индивидуальных голосов в карантине архива
    /// </summary>
    public long QuarantineDeputyVotes { get; set; }

    /// <summary>
    /// Признак того, что все контрольные суммы совпали
    /// </summary>
    public bool IsSuccess => Rows.All(x => x.IsMatch);

    /// <summary>
    /// Текстовое представление отчёта
    /// </summary>
    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Сверка контрольных сумм с паспортом архива (Docs/02-data-sources.md, §2.2)");
        if (QuarantineVotes > 0)
        {
            builder.AppendLine("Факт = основная база + карантин архива (аномальные файлы учтены по сырому JSON)");
        }

        builder.AppendLine();
        foreach (var row in Rows)
        {
            var status = row.IsMatch ? "OK" : "РАСХОЖДЕНИЕ";
            builder.AppendLine($"{row.Name,-55} {row.Expected,15:N0} {row.Actual,15:N0}  {status}");
        }

        builder.AppendLine();
        var mismatchCount = Rows.Count(x => !x.IsMatch);
        builder.AppendLine(IsSuccess
            ? "Итог: все контрольные суммы совпадают"
            : $"Итог: расхождения в {mismatchCount} метриках");
        builder.AppendLine($"Основная база: {DbVotes:N0} голосований, {DbDeputyVotes:N0} индивидуальных голосов");
        builder.AppendLine($"Карантин архива: {QuarantineVotes:N0} голосований, {QuarantineDeputyVotes:N0} индивидуальных голосов");
        return builder.ToString();
    }
}