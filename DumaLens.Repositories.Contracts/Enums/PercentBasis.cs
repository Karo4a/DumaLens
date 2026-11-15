namespace DumaLens.Repositories.Contracts.Enums;

/// <summary>
/// База процента в фракционном условии поиска
/// </summary>
public enum PercentBasis
{
    /// <summary>
    /// От списочного состава фракции (MemberCount)
    /// </summary>
    Members = 0,

    /// <summary>
    /// От проголосовавших членов фракции (AcceptCount + DeclineCount + AbstainCount)
    /// </summary>
    Voted = 1,
}