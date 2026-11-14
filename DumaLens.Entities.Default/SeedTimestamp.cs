namespace DumaLens.Entities.Default;

/// <summary>
/// Фиксированная отметка времени для seed-данных
/// </summary>
/// <remarks>
/// HasData требует неизменных значений между построениями модели, поэтому аудит seed-записей
/// заполняется константой, а не текущим временем.
/// </remarks>
public static class SeedTimestamp
{
    /// <summary>
    /// Отметка времени создания и изменения seed-записей
    /// </summary>
    public static readonly DateTimeOffset Value = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}