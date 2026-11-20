using DumaLens.Common.Contracts;
using DumaLens.Repositories.Contracts.WriteRepositories;
using DumaLens.Services.Contracts;

namespace DumaLens.Services.Auth;

/// <inheritdoc cref="ITokenCleanupService"/>
/// <remarks>
/// Ротация refresh-токенов только растит таблицу: каждый login/refresh вставляет новую запись.
/// Очистка удаляет записи, утратившие силу давнее срока хранения (Auth:RefreshTokenRetentionDays):
/// истёкшие (ExpiresAt) и отозванные (RevokedAt). Активные и недавно отозванные токены сохраняются —
/// недавно отозванные нужны для обнаружения кражи (grace-окно и разбор инцидентов).
/// </remarks>
public class TokenCleanupService : ITokenCleanupService, IServiceAnchor
{
    private const int BatchSize = 5000;

    private readonly IRefreshTokenWriteRepository refreshTokenWriteRepository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly AuthOptions options;

    /// <summary>
    /// Конструктор
    /// </summary>
    public TokenCleanupService(
        IRefreshTokenWriteRepository refreshTokenWriteRepository,
        IDateTimeProvider dateTimeProvider,
        AuthOptions options)
    {
        this.refreshTokenWriteRepository = refreshTokenWriteRepository;
        this.dateTimeProvider = dateTimeProvider;
        this.options = options;
    }

    async Task<int> ITokenCleanupService.CleanupAsync(CancellationToken cancellationToken)
    {
        var cutoff = dateTimeProvider.UtcNow().AddDays(-options.RefreshTokenRetentionDays);
        return await refreshTokenWriteRepository.DeleteOldTokensAsync(cutoff, BatchSize, cancellationToken);
    }
}