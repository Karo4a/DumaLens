using DumaLens.Context.Contracts;
using DumaLens.Entities.Configurations;
using Microsoft.EntityFrameworkCore;
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