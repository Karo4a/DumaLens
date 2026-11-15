using DumaLens.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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

    internal ApiSeed Seed { get; private set; } = null!;

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
    /// Создаёт HTTP-клиент тестового хоста
    /// </summary>
    internal HttpClient CreateClient()
        => factory.CreateClient();

    /// <summary>
    /// Асинхронная инициализация: создаёт одноразовую базу, если PostgreSQL доступен
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!PostgreSqlAvailability.IsAvailable)
        {
            return;
        }
        // Э1: схема накатывается миграциями (InitialCreate)
        await Context.Database.MigrateAsync();
        databaseCreated = true;
        Seed = new ApiSeed(Context);
        await Seed.SeedAsync();
    }

    /// <summary>
    /// Асинхронное освобождение: удаляет одноразовую базу
    /// </summary>
    public async Task DisposeAsync()
    {
        if (databaseCreated)
        {
            // Явный DROP WITH (FORCE): EnsureDeletedAsync падает на открытых соединениях пула Npgsql
            var database = Context.Database.GetDbConnection().Database;
            await using (var connection = new NpgsqlConnection(PostgreSqlAvailability.MaintenanceConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "DROP DATABASE IF EXISTS \"" + database + "\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync();
            }
            await Context.Database.CloseConnectionAsync();
        }
        if (context != null)
        {
            await context.DisposeAsync();
        }
        await factory.DisposeAsync();
    }
}