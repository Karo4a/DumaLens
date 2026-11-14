using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IConvocationReadRepository" />
public class ConvocationReadRepository : IConvocationReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ConvocationReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    Task<ConvocationDbModel?> IConvocationReadRepository.GetById(Guid id, CancellationToken cancellationToken)
        => reader.Read<Convocation>()
            .NotDeletedAt()
            .ById(id)
            .Select(x => new ConvocationDbModel
            {
                Id = x.Id,
                Number = x.Number,
                SiteCode = x.SiteCode,
                Title = x.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .FirstOrDefaultAsync(cancellationToken);

    Task<ConvocationDbModel?> IConvocationReadRepository.GetByNumber(int number, CancellationToken cancellationToken)
        => reader.Read<Convocation>()
            .NotDeletedAt()
            .Where(x => x.Number == number)
            .Select(x => new ConvocationDbModel
            {
                Id = x.Id,
                Number = x.Number,
                SiteCode = x.SiteCode,
                Title = x.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .FirstOrDefaultAsync(cancellationToken);

    Task<IReadOnlyCollection<ConvocationDbModel>> IConvocationReadRepository.GetAll(CancellationToken cancellationToken)
        => reader.Read<Convocation>()
            .NotDeletedAt()
            .Select(x => new ConvocationDbModel
            {
                Id = x.Id,
                Number = x.Number,
                SiteCode = x.SiteCode,
                Title = x.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .OrderBy(x => x.Number)
            .ToReadOnlyCollectionAsync(cancellationToken);
}