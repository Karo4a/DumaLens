using DumaLens.Context.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DumaLens.Context.Tests;

/// <summary>
/// Абстрактный класс для тестов с базой в памяти
/// </summary>
public abstract class DumaLensContextInMemory : IAsyncDisposable
{
    /// <summary>
    /// Контекст <see cref="DumaLensContext"/>
    /// </summary>
    protected DumaLensContext Context { get; }

    /// <inheritdoc cref="IUnitOfWork"/>
    protected IUnitOfWork UnitOfWork => Context;

    /// <summary>
    /// Конструктор
    /// </summary>
    protected DumaLensContextInMemory()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DumaLensContext>()
            .UseInMemoryDatabase($"DumaLensContext{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        Context = new DumaLensContext(optionsBuilder.Options);
    }

    // Prepare*-хелперы появятся на Э1 (сущности справочников)

    /// <inheritdoc cref="IAsyncDisposable"/>
    public async ValueTask DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}