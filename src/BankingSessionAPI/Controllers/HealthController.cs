using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace BankingSessionAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Vérifie l'état de santé de l'API
    /// </summary>
    /// <returns>État de santé de l'API et de ses dépendances</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTime.UtcNow,
                totalDuration = healthReport.TotalDuration.TotalMilliseconds,
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                    data = entry.Value.Data
                })
            };

            return healthReport.Status == HealthStatus.Healthy 
                ? Ok(response) 
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de l'état de santé");
            
            var errorResponse = new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = "Erreur lors de la vérification de l'état de santé"
            };

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }

    /// <summary>
    /// Vérifie l'état de santé simplifié de l'API
    /// </summary>
    /// <returns>Simple confirmation que l'API fonctionne</returns>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new 
        { 
            message = "API Banking Session is running", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Vérifie l'état de santé détaillé avec métriques
    /// </summary>
    /// <returns>État de santé détaillé avec métriques système</returns>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDetailed()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            // Métriques système additionnelles
            var gcMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;
            var processorCount = Environment.ProcessorCount;
            var osVersion = Environment.OSVersion.ToString();
            var clrVersion = Environment.Version.ToString();

            var response = new
            {
                api = new
                {
                    status = healthReport.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                    version = "1.0.0",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                },
                system = new
                {
                    operatingSystem = osVersion,
                    clrVersion = clrVersion,
                    processorCount = processorCount,
                    workingSetMemory = $"{workingSet / 1024 / 1024} MB",
                    gcMemory = $"{gcMemory / 1024 / 1024} MB",
                    machineName = Environment.MachineName
                },
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = $"{entry.Value.Duration.TotalMilliseconds} ms",
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                    data = entry.Value.Data
                }),
                performance = new
                {
                    totalDuration = $"{healthReport.TotalDuration.TotalMilliseconds} ms",
                    averageCheckDuration = healthReport.Entries.Any() 
                        ? $"{healthReport.Entries.Average(e => e.Value.Duration.TotalMilliseconds)} ms" 
                        : "0 ms"
                }
            };

            return healthReport.Status == HealthStatus.Healthy 
                ? Ok(response) 
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de l'état de santé détaillé");
            
            var errorResponse = new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = "Erreur lors de la vérification de l'état de santé détaillé",
                exception = ex.Message
            };

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }
}