namespace DumaLens.Importer.Import;

/// <summary>
/// Параметры импорта архива
/// </summary>
public sealed class ImportOptions
{
    /// <summary>
    /// Путь к архиву vote-master.zip
    /// </summary>
    public string ZipPath { get; set; } = string.Empty;

    /// <summary>
    /// Размер пачки голосований в одной транзакции
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Пропускать голосования, уже загруженные в базу (продолжение после сбоя)
    /// </summary>
    public bool Resume { get; set; }

    /// <summary>
    /// Ограничение числа файлов архива для обработки (0 — без ограничения); для пробных прогонов
    /// </summary>
    public int MaxFiles { get; set; }

    /// <summary>
    /// Bulk-режим для пустых таблиц: вторичные индексы и FK потоковых таблиц удаляются на время
    /// импорта и восстанавливаются после (существенно быстрее COPY); требует пустых DeputyVotes и VoteFactionStats
    /// </summary>
    public bool Bulk { get; set; }

    /// <summary>
    /// Число параллельных задач разбора JSON (0 — автоматически по числу процессоров)
    /// </summary>
    public int Workers { get; set; }
}