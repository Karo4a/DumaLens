using DumaLens.Common.Contracts;

namespace DumaLens.Services.Tests.Infrastructure;

/// <summary>
/// Управляемые часы для тестов: время можно сдвигать (окна блокировки, сроки токенов)
/// </summary>
public sealed class TestDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="start">Начальный момент времени</param>
    public TestDateTimeProvider(DateTimeOffset start)
    {
        Time = start;
    }

    /// <summary>
    /// Текущий момент времени (_utc)
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <inheritdoc cref="IDateTimeProvider.UtcNow"/>
    public DateTimeOffset UtcNow() => Time;

    /// <inheritdoc cref="IDateTimeProvider.Now"/>
    public DateTimeOffset Now() => Time;
}