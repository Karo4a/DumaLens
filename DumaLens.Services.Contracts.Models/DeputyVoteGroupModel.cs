namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Голоса депутатов одной фракции в голосовании
/// </summary>
public class DeputyVoteGroupModel
{
    /// <summary>
    /// Код канонической фракции (null — депутаты без фракции)
    /// </summary>
    public string? FactionCode { get; set; }

    /// <summary>
    /// Историческое название фракции на момент голосования (null — без фракции)
    /// </summary>
    public string? FactionTitle { get; set; }

    /// <summary>
    /// Голоса депутатов фракции
    /// </summary>
    public IReadOnlyCollection<DeputyVoteItemModel> Deputies { get; set; } = [];
}