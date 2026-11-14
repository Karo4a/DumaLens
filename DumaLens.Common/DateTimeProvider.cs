using DumaLens.Common.Contracts;

namespace DumaLens.Common;

/// <inheritdoc cref="IDateTimeProvider"/>
public class DateTimeProvider : IDateTimeProvider
{
    DateTimeOffset IDateTimeProvider.Now()
        => DateTimeOffset.Now;

    DateTimeOffset IDateTimeProvider.UtcNow()
        => DateTimeOffset.UtcNow;
}