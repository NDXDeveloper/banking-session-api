using BankingSessionAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using BankingSessionAPI.Configuration;

namespace BankingSessionAPI.Services.Background;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly RedisSettings _redisSettings;
    private readonly TimeSpan _period;

    public SessionCleanupService(
        IServiceProvider serviceProvider, 
        ILogger<SessionCleanupService> logger,
        IOptions<RedisSettings> redisSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _redisSettings = redisSettings.Value;
        _period = TimeSpan.FromMinutes(_redisSettings.SessionCleanupIntervalInMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_redisSettings.EnableSessionCleanup)
        {
            _logger.LogInformation("Session cleanup is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Session cleanup service running at: {time}", DateTimeOffset.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
                
                await sessionService.CleanupExpiredSessionsAsync();
                
                _logger.LogInformation("Session cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
            
            await Task.Delay(_period, stoppingToken);
        }
    }
}