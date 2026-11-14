namespace DumaLens.Importer.Verify;

/// <summary>
/// Контрольные суммы созыва из паспорта архива
/// </summary>
/// <param name="Votes">Число голосований</param>
/// <param name="DeputyVotes">Число индивидуальных голосов депутатов</param>
public readonly record struct ConvocationChecksum(long Votes, long DeputyVotes);