namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Карточка депутата для чтения из БД
/// </summary>
public class DeputyCardDbModel
{
    /// <summary>
    /// Идентификатор депутата
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор депутата на сайте Госдумы
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Полное имя депутата
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Имя депутата для сортировки (фамилия впереди)
    /// </summary>
    public string SortName { get; set; } = string.Empty;

    /// <summary>
    /// Членство во фракциях по созывам
    /// </summary>
    public IReadOnlyCollection<DeputyMembershipDbModel> Memberships { get; set; } = [];

    /// <summary>
    /// Число голосов «за»
    /// </summary>
    public int AcceptCount { get; set; }

    /// <summary>
    /// Число голосов «против»
    /// </summary>
    public int DeclineCount { get; set; }

    /// <summary>
    /// Число голосов «воздержался»
    /// </summary>
    public int AbstainCount { get; set; }

    /// <summary>
    /// Число голосов «не голосовал»
    /// </summary>
    public int NoneCount { get; set; }
}