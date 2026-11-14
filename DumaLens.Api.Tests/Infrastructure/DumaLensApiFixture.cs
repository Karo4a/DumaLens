using DumaLens.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DumaLens.Api.Tests.Infrastructure;

/// <summary>
/// Фикстура API для интеграционных тестов с PostgreSQL (ADR-3)
/// </summary>
public class DumaLensApiFixture : IAsyncLifetime
{
    private readonly TestWebApplicationFactory factory;
    private DumaLensContext? context;
    private bool databaseCreated;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DumaLensApiFixture()
    {
        factory = new TestWebApplicationFactory();
    }

    /// <summary>
    /// Контекст одноразовой базы данных теста
    /// </summary>
    internal DumaLensContext Context
    {
        get
        {
            if (context != null)
            {
                return context;
            }
            var scope = factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
            context = scope.ServiceProvider.GetRequiredService<DumaLensContext>();
            return context;
        }
    }

    /// <summary>
    /// Асинхронная инициализация: создаёт одноразовую базу, если PostgreSQL доступен
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!PostgreSqlAvailability.IsAvailable)
        {
            return;
        }
        // Э0: модель пуста — EnsureCreated; с Э1 (миграции) — MigrateAsync
        await Context.Database.EnsureCreatedAsync();
        databaseCreated = true;
    }

    /// <summary>
    /// Асинхронное освобождение: удаляет одноразовую базу
    /// </summary>
    public async Task DisposeAsync()
    {
        if (databaseCreated)
        {
            await Context.Database.EnsureDeletedAsync();
            await Context.Database.CloseConnectionAsync();
        }
        if (context != null)
        {
            await context.DisposeAsync();
        }
        await factory.DisposeAsync();
    }
}