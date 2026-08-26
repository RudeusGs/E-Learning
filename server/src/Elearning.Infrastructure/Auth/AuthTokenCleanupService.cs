using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elearning.Infrastructure.Auth;

public sealed class AuthTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AuthTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan RefreshTokenRetention = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                TokenCleanupFailed(logger, exception);
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var now = timeProvider.GetUtcNow();

        await dbContext.BlacklistedTokens
            .Where(token => token.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var refreshCutoff = now - RefreshTokenRetention;
        await dbContext.RefreshTokens
            .Where(token => token.ExpiresAtUtc <= refreshCutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static readonly Action<ILogger, Exception?> TokenCleanupFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(AuthenticationLogEventIds.TokenCleanupFailed, nameof(TokenCleanupFailed)),
        "Authentication token cleanup failed; it will retry later.");
}
