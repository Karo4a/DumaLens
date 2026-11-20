namespace DumaLens.Web.Infrastructure;

/// <summary>
/// Общий семафор обновления токенов на цепь Blazor (scoped): все NSwag-клиенты цепи получают
/// один экземпляр, поэтому параллельные запросы разных клиентов выполняют ровно один refresh
/// </summary>
public sealed class RefreshLock : IDisposable
{
    /// <summary>
    /// Семафор: одновременно не более одного обновления пары токенов на цепь
    /// </summary>
    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
        => Semaphore.Dispose();
}