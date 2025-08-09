using System.Diagnostics.Metrics;
using BankingSessionAPI.Configuration;
using BankingSessionAPI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace BankingSessionAPI.Services.Implementation;

/// <summary>
/// Service de gestion des métriques Prometheus
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly MonitoringSettings _monitoringSettings;
    private readonly ILogger<MetricsService> _logger;
    
    // Métriques Prometheus
    private readonly Counter<int> _loginAttemptsCounter;
    private readonly Counter<int> _twoFactorAttemptsCounter;
    private readonly UpDownCounter<int> _activeSessionsGauge;
    private readonly Histogram<double> _responseTimeHistogram;
    private readonly Counter<int> _errorsCounter;
    private readonly Counter<int> _sessionExtensionsCounter;
    private readonly Histogram<double> _databaseOperationHistogram;
    private readonly Histogram<double> _redisOperationHistogram;

    public MetricsService(IOptions<MonitoringSettings> monitoringSettings, ILogger<MetricsService> logger)
    {
        _monitoringSettings = monitoringSettings.Value;
        _logger = logger;

        if (!_monitoringSettings.EnablePrometheusMetrics)
        {
            _logger.LogInformation("Métriques Prometheus désactivées");
            return;
        }

        var meter = new Meter("BankingSessionAPI");

        _loginAttemptsCounter = meter.CreateCounter<int>(
            "banking_login_attempts_total",
            "Total number of login attempts");

        _twoFactorAttemptsCounter = meter.CreateCounter<int>(
            "banking_2fa_attempts_total", 
            "Total number of 2FA verification attempts");

        _activeSessionsGauge = meter.CreateUpDownCounter<int>(
            "banking_active_sessions",
            "Current number of active sessions");

        _responseTimeHistogram = meter.CreateHistogram<double>(
            "banking_http_request_duration_ms",
            "Duration of HTTP requests in milliseconds");

        _errorsCounter = meter.CreateCounter<int>(
            "banking_errors_total",
            "Total number of errors");

        _sessionExtensionsCounter = meter.CreateCounter<int>(
            "banking_session_extensions_total",
            "Total number of session extension attempts");

        _databaseOperationHistogram = meter.CreateHistogram<double>(
            "banking_database_operation_duration_ms",
            "Duration of database operations in milliseconds");

        _redisOperationHistogram = meter.CreateHistogram<double>(
            "banking_redis_operation_duration_ms", 
            "Duration of Redis operations in milliseconds");

        _logger.LogInformation("Métriques Prometheus initialisées");
    }

    public void IncrementLoginAttempt(string result, string userAgent = "", string ipAddress = "")
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        var tags = new TagList
        {
            { "result", result },
            { "user_agent", userAgent.Length > 50 ? userAgent[..50] : userAgent },
            { "ip_prefix", GetIpPrefix(ipAddress) }
        };

        _loginAttemptsCounter.Add(1, tags);
    }

    public void IncrementTwoFactorAttempt(string result)
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        _twoFactorAttemptsCounter.Add(1, new TagList { { "result", result } });
    }

    public void UpdateActiveSessions(int count)
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        _activeSessionsGauge.Add(count);
    }

    public void RecordResponseTime(string endpoint, string method, int statusCode, double durationMs)
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        var tags = new TagList
        {
            { "endpoint", endpoint },
            { "method", method },
            { "status_code", statusCode.ToString() }
        };

        _responseTimeHistogram.Record(durationMs, tags);
    }

    public void IncrementError(string endpoint, string errorType)
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        var tags = new TagList
        {
            { "endpoint", endpoint },
            { "error_type", errorType }
        };

        _errorsCounter.Add(1, tags);
    }

    public void IncrementSessionExtension(string result)
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        _sessionExtensionsCounter.Add(1, new TagList { { "result", result } });
    }

    public void UpdateDatabaseMetrics(string operation, bool success, double durationMs)
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        var tags = new TagList
        {
            { "operation", operation },
            { "success", success.ToString().ToLower() }
        };

        _databaseOperationHistogram.Record(durationMs, tags);
    }

    public void UpdateRedisMetrics(string operation, bool success, double durationMs)
    {
        if (!_monitoringSettings.EnablePrometheusMetrics) return;

        var tags = new TagList
        {
            { "operation", operation },
            { "success", success.ToString().ToLower() }
        };

        _redisOperationHistogram.Record(durationMs, tags);
    }

    /// <summary>
    /// Anonymise l'adresse IP en gardant seulement le préfixe
    /// </summary>
    private static string GetIpPrefix(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return "unknown";
        
        var parts = ipAddress.Split('.');
        if (parts.Length >= 2)
        {
            return $"{parts[0]}.{parts[1]}.x.x";
        }
        
        return "unknown";
    }
}