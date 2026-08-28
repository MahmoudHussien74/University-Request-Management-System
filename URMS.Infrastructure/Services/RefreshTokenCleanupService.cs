using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace URMS.Infrastructure.Services;

/// <summary>
/// Background service that periodically cleans up expired and revoked refresh tokens
/// to prevent the RefreshTokens table from growing unbounded over time.
/// Runs once every 24 hours.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);
    private const int ExpiredTokenRetentionDays = 30;

    public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to let the app fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — expected
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired refresh tokens");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-ExpiredTokenRetentionDays);

        var deletedCount = await context.RefreshTokens
            .Where(t => t.IsRevoked || t.ExpiresOn < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Refresh token cleanup completed: removed {DeletedCount} expired/revoked tokens (cutoff: {CutoffDate:yyyy-MM-dd})",
                deletedCount, cutoffDate);
        }
    }
}
