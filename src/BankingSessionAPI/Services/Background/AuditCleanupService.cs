using BankingSessionAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using BankingSessionAPI.Configuration;

namespace BankingSessionAPI.Services.Background;

public class AuditCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditCleanupService> _logger;
    private readonly AuditSettings _auditSettings;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public AuditCleanupService(
        IServiceProvider serviceProvider,
        ILogger<AuditCleanupService> logger,
        IOptions<AuditSettings> auditSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _auditSettings = auditSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Audit cleanup service running at: {time}", DateTimeOffset.Now);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                
                // TODO: Implémenter DeleteOldAuditLogsAsync dans IAuditService
                _logger.LogInformation("Audit cleanup service executed (cleanup logic to be implemented)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit cleanup service");
            }
            
            await Task.Delay(_period, stoppingToken);
        }
    }
}