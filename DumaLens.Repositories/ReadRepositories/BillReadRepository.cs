using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IBillReadRepository"/>
public class BillReadRepository : IBillReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public BillReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    Task<BillDbModel?> IBillReadRepository.GetByNumber(string number, CancellationToken cancellationToken)
        => reader.Read<Bill>()
            .NotDeletedAt()
            .Where(x => x.Number == number)
            .Select(x => new BillDbModel
            {
                Id = x.Id,
                Number = x.Number,
            })
            .FirstOrDefaultAsync(cancellationToken);
}