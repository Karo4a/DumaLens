using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис карточек фракций
/// </summary>
public interface IFactionCardService
{
    /// <summary>
    /// Получает канонические фракции с историческими периодами
    /// </summary>
    Task<IReadOnlyCollection<FactionCardModel>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Получает карточку фракции с периодами и сводной статистикой по созывам
    /// </summary>
    /// <exception cref="Exceptions.DumaLensNotFoundException">Фракция не найдена</exception>
    Task<FactionCardModel> GetCard(string code, CancellationToken cancellationToken);
}