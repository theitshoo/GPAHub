using GPAHub.Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GPAHub.Infrastructure.Persistence;

public class RefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await PurgeOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Refresh token cleanup cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task PurgeOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        var cutoff = DateTimeOffset.UtcNow - RefreshTokenPurgeDefaults.ExpiredRetention;
        var revokedCutoff = DateTimeOffset.UtcNow - RevokedRetention;

        var purgedExpired = await repository.PurgeExpiredAsync(cutoff, cancellationToken);
        var purgedRevoked = await repository.PurgeRevokedAsync(revokedCutoff, cancellationToken);

        if (purgedExpired + purgedRevoked > 0)
        {
            _logger.LogInformation(
                "Refresh token purge removed {Expired} expired and {Revoked} revoked tokens",
                purgedExpired, purgedRevoked);
        }
    }
}

public static class RefreshTokenPurgeDefaults
{
    public static TimeSpan ExpiredRetention { get; } = TimeSpan.FromDays(1);
}

