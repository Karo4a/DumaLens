namespace DumaLens.Importer.Import;

/// <summary>
/// Строка индивидуального голоса для массовой вставки через COPY
/// </summary>
/// <param name="VoteId">Идентификатор голосования</param>
/// <param name="DeputyId">Идентификатор депутата</param>
/// <param name="FactionPeriodId">Идентификатор фракционного периода; null — фракция неизвестна</param>
/// <param name="Result">Код результата голосования</param>
public readonly record struct DeputyVoteRow(Guid VoteId, Guid DeputyId, Guid? FactionPeriodId, short Result);