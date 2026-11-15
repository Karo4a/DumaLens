namespace DumaLens.Api.Models.Deputies;

/// <summary>
/// Карточка депутата
/// </summary>
public class DeputyApiModel
{
    /// <summary>
    /// Идентификатор депутата на сайте Госдумы
    /// </summary>
    public long Id { get; set; }

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
    public IReadOnlyCollection<DeputyMembershipApiModel> Factions { get; set; } = [];

    /// <summary>
    /// Общее число голосований с участием депутата
    /// </summary>
    public int TotalVotes { get; set; }

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

    /// <summary>
    /// Доля участия в голосованиях в процентах (null — голосований нет)
    /// </summary>
    public decimal? ParticipationPct { get; set; }
}