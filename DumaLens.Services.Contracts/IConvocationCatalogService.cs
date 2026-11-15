using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис каталога созывов
/// </summary>
public interface IConvocationCatalogService
{
    /// <summary>
    /// Получает справочник созывов с числом голосований
    /// </summary>
    Task<IReadOnlyCollection<ConvocationCatalogItemModel>> GetAll(CancellationToken cancellationToken);
}