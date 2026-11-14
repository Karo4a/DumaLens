using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Diagnostics.CodeAnalysis;

namespace DumaLens.Context;

/// <summary>
/// Контекст базы данных
/// </summary>
public class DumaLensContext : DbContext, IReader, IWriter, IUnitOfWork
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public DumaLensContext(DbContextOptions<DumaLensContext> options)
        : base(options) { }

    /// <inheritdoc cref="DbContext.OnModelCreating"/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IEntityConfiguration).Assembly);
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            // InMemory-провайдер не поддерживает NpgsqlTsVector; генерируемая колонка нужна только на PostgreSQL
            modelBuilder.Entity<Vote>().Ignore("TitleSearch");
        }
        UseUtcDateTimeOffset(modelBuilder);
    }

    /// <summary>
    /// Нормализует все DateTimeOffset-свойства к UTC при записи
    /// </summary>
    /// <remarks>
    /// Npgsql принимает в timestamptz только DateTimeOffset со смещением 0;
    /// историческое смещение источника хранится отдельно (например, Vote.UtcOffsetMinutes).
    /// </remarks>
    private static void UseUtcDateTimeOffset(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            value => value.ToUniversalTime(),
            value => value);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(converter);
                }
            }
        }
    }

    IQueryable<TEntity> IReader.Read<TEntity>()
        where TEntity : class
        => base.Set<TEntity>()
            .AsNoTracking()
            .AsQueryable();

    void IWriter.Add<TEntity>([NotNull] TEntity entity)
        => base.Entry(entity).State = EntityState.Added;

    void IWriter.Edit<TEntity>([NotNull] TEntity entity)
        => base.Entry(entity).State = EntityState.Modified;

    void IWriter.Delete<TEntity>([NotNull] TEntity entity)
        => base.Entry(entity).State = EntityState.Deleted;

    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        var count = await base.SaveChangesAsync(cancellationToken);
        foreach (var entry in base.ChangeTracker.Entries().ToArray())
        {
            entry.State = EntityState.Detached;
        }
        return count;
    }
}