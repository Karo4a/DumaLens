using Ahatornn.TestGenerator;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Enums;
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

    // Счётчики вместо Random: InMemory проверяет альтернативные ключи, случайные RowId могут столкнуться
    private short nextPeriodRowId = 1000;
    private int nextDeputyRowId = 100000;

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

    /// <summary>
    /// Подготавливает <see cref="Convocation"/> в БД для тестов
    /// </summary>
    public async Task<Convocation> PrepareConvocation(DateTimeOffset? deletedAt = null, bool save = true)
    {
        var number = Random.Shared.Next(100, 100000);
        var convocation = TestEntityProvider.Shared.Create<Convocation>(x =>
        {
            x.Number = number;
            x.SiteCode = $"TEST{number}";
            x.DateFrom = new DateOnly(2020, 1, 1);
            x.DateTo = null;
            x.DeletedAt = deletedAt;
        });
        if (save)
        {
            await Context.AddAsync(convocation);
            await UnitOfWork.SaveChangesAsync();
        }
        return convocation;
    }

    /// <summary>
    /// Подготавливает <see cref="Faction"/> в БД для тестов
    /// </summary>
    public async Task<Faction> PrepareFaction(DateTimeOffset? deletedAt = null, bool save = true)
    {
        var faction = TestEntityProvider.Shared.Create<Faction>(x =>
        {
            x.Code = Guid.NewGuid().ToString("N")[..16];
            x.DeletedAt = deletedAt;
        });
        if (save)
        {
            await Context.AddAsync(faction);
            await UnitOfWork.SaveChangesAsync();
        }
        return faction;
    }

    /// <summary>
    /// Подготавливает <see cref="FactionPeriod"/> в БД для тестов
    /// </summary>
    public async Task<FactionPeriod> PrepareFactionPeriod(DateTimeOffset? deletedAt = null, bool save = true)
    {
        var faction = await PrepareFaction(save: !save);
        var convocation = await PrepareConvocation(save: !save);
        var period = TestEntityProvider.Shared.Create<FactionPeriod>(x =>
        {
            x.RowId = nextPeriodRowId++;
            x.FactionId = faction.Id;
            x.ConvocationId = convocation.Id;
            x.SiteId = Random.Shared.NextInt64(100000, 99999999);
            x.DateFrom = new DateOnly(2020, 1, 1);
            x.DateTo = null;
            x.Faction = faction;
            x.Convocation = convocation;
            x.DeletedAt = deletedAt;
        });
        if (save)
        {
            await Context.AddAsync(period);
            await UnitOfWork.SaveChangesAsync();
        }
        return period;
    }

    /// <summary>
    /// Подготавливает <see cref="Deputy"/> в БД для тестов
    /// </summary>
    public async Task<Deputy> PrepareDeputy(DateTimeOffset? deletedAt = null, bool save = true)
    {
        var deputy = TestEntityProvider.Shared.Create<Deputy>(x =>
        {
            x.RowId = nextDeputyRowId++;
            x.SiteId = Random.Shared.NextInt64(100000, 99999999);
            x.DeletedAt = deletedAt;
        });
        if (save)
        {
            await Context.AddAsync(deputy);
            await UnitOfWork.SaveChangesAsync();
        }
        return deputy;
    }

    /// <summary>
    /// Подготавливает <see cref="UserRole"/> в БД для тестов
    /// </summary>
    public async Task<UserRole> PrepareUserRole(Role role = Role.User, DateTimeOffset? deletedAt = null, bool save = true)
    {
        var userRole = TestEntityProvider.Shared.Create<UserRole>(x =>
        {
            x.Role = role;
            x.DeletedAt = deletedAt;
        });
        if (save)
        {
            await Context.AddAsync(userRole);
            await UnitOfWork.SaveChangesAsync();
        }
        return userRole;
    }

    /// <inheritdoc cref="IAsyncDisposable"/>
    public async ValueTask DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}