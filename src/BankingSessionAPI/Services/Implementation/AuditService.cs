using BankingSessionAPI.Models.DTOs;
using BankingSessionAPI.Services.Interfaces;
using BankingSessionAPI.Data;
using BankingSessionAPI.Models.Entities;
using BankingSessionAPI.Constants;
using BankingSessionAPI.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AutoMapper;
using System.Text.Json;

namespace BankingSessionAPI.Services.Implementation;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditSettings _auditSettings;
    private readonly IMapper _mapper;

    public AuditService(
        ApplicationDbContext context,
        ILogger<AuditService> logger,
        IOptions<AuditSettings> auditSettings,
        IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _auditSettings = auditSettings.Value;
        _mapper = mapper;
    }

    public async Task LogAsync(
        string action, 
        string entityType, 
        string? entityId, 
        string userId, 
        string userName, 
        string? ipAddress, 
        string? userAgent, 
        string? sessionId, 
        object? oldValues = null, 
        object? newValues = null, 
        string? additionalInfo = null, 
        bool isSuccessful = true, 
        string? errorMessage = null)
    {
        if (!_auditSettings.EnableAuditLogging)
            return;

        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage,
                SessionId = sessionId,
                Level = isSuccessful ? "Information" : "Error"
            };

            // Sérialiser les données si nécessaire et si autorisé
            if (_auditSettings.LogSensitiveData)
            {
                if (oldValues != null || newValues != null || additionalInfo != null)
                {
                    var data = new
                    {
                        OldValues = oldValues,
                        NewValues = newValues,
                        AdditionalInfo = additionalInfo
                    };
                    auditLog.AdditionalInfo = JsonSerializer.Serialize(data);
                }
            }
            else if (additionalInfo != null)
            {
                auditLog.AdditionalInfo = additionalInfo;
            }

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} by {UserName} ({UserId}) at {Timestamp}", 
                action, userName, userId, auditLog.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du log d'audit pour l'action {Action} par {UserName}", 
                action, userName);
        }
    }

    public async Task LogLoginAsync(string userId, string userName, string ipAddress, string userAgent, string sessionId, bool isSuccessful, string? errorMessage = null)
    {
        await LogAsync(
            AuditActions.Login,
            "Session",
            sessionId,
            userId,
            userName,
            ipAddress,
            userAgent,
            sessionId,
            additionalInfo: $"Login attempt from {ipAddress}",
            isSuccessful: isSuccessful,
            errorMessage: errorMessage);
    }

    public async Task LogLogoutAsync(string userId, string userName, string ipAddress, string? userAgent, string sessionId)
    {
        await LogAsync(
            AuditActions.Logout,
            "Session",
            sessionId,
            userId,
            userName,
            ipAddress,
            userAgent,
            sessionId,
            additionalInfo: $"Logout from {ipAddress}",
            isSuccessful: true);
    }

    public async Task LogPasswordChangeAsync(string userId, string userName, string ipAddress, string sessionId, bool isSuccessful, string? errorMessage = null)
    {
        await LogAsync(
            AuditActions.PasswordChanged,
            "User",
            userId,
            userId,
            userName,
            ipAddress,
            null,
            sessionId,
            additionalInfo: "Password change attempt",
            isSuccessful: isSuccessful,
            errorMessage: errorMessage);
    }

    public async Task LogUserActionAsync(string action, string userId, string userName, string ipAddress, string? userAgent, string? sessionId, object? additionalData = null)
    {
        await LogAsync(
            action,
            "User",
            userId,
            userId,
            userName,
            ipAddress,
            userAgent,
            sessionId,
            additionalInfo: additionalData?.ToString());
    }

    public async Task LogSecurityEventAsync(string action, string userId, string userName, string ipAddress, string? userAgent, string? sessionId, string details, string level = "Warning")
    {
        if (!_auditSettings.EnableAuditLogging)
            return;

        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = "Security",
                EntityId = null,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow,
                IsSuccessful = level != "Error",
                ErrorMessage = level == "Error" ? details : null,
                SessionId = sessionId,
                Level = level,
                AdditionalInfo = details
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security event logged: {Action} by {UserName} - {Details}", 
                action, userName, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du log de sécurité pour {Action}", action);
        }
    }

    public async Task LogSystemEventAsync(string action, string details, string level = "Information")
    {
        if (!_auditSettings.EnableAuditLogging)
            return;

        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = "System",
                UserName = "System",
                Action = action,
                EntityType = "System",
                EntityId = null,
                IpAddress = "Internal",
                UserAgent = "System",
                Timestamp = DateTime.UtcNow,
                IsSuccessful = level != "Error",
                ErrorMessage = level == "Error" ? details : null,
                SessionId = null,
                Level = level,
                AdditionalInfo = details
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("System event logged: {Action} - {Details}", action, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du log système pour {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(
        string? userId = null, 
        string? action = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        int skip = 0, 
        int take = 50)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(x => x.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action.Contains(action));

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            var auditLogs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AuditLogDto>>(auditLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des logs d'audit");
            return Enumerable.Empty<AuditLogDto>();
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetUserAuditLogsAsync(
        string userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        int skip = 0, 
        int take = 50)
    {
        return await GetAuditLogsAsync(userId, null, startDate, endDate, skip, take);
    }

    public async Task<IEnumerable<AuditLogDto>> GetSecurityAuditLogsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        int skip = 0, 
        int take = 50)
    {
        try
        {
            var query = _context.AuditLogs
                .Where(x => x.EntityType == "Security" || x.Level == "Warning" || x.Level == "Error");

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            var auditLogs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AuditLogDto>>(auditLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des événements de sécurité");
            return Enumerable.Empty<AuditLogDto>();
        }
    }

    public async Task<int> GetAuditLogsCountAsync(
        string? userId = null, 
        string? action = null, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(x => x.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action.Contains(action));

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du comptage des logs d'audit");
            return 0;
        }
    }

    public async Task<Dictionary<string, int>> GetAuditStatisticsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            var statistics = await query
                .GroupBy(x => x.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération des statistiques d'audit");
            return new Dictionary<string, int>();
        }
    }

    public async Task CleanupOldAuditLogsAsync(int retentionDays = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var oldLogs = _context.AuditLogs.Where(x => x.Timestamp < cutoffDate);
            
            var count = await oldLogs.CountAsync();
            if (count > 0)
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Suppression de {Count} anciens logs d'audit antérieurs au {CutoffDate}", 
                    count, cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du nettoyage des anciens logs d'audit");
        }
    }

    public Task<bool> IsAuditingEnabledAsync()
    {
        return Task.FromResult(_auditSettings.EnableAuditLogging);
    }

    public async Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id)
    {
        try
        {
            var auditLog = await _context.AuditLogs
                .FirstOrDefaultAsync(x => x.Id == id);

            return auditLog != null ? _mapper.Map<AuditLogDto>(auditLog) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du log d'audit {Id}", id);
            return null;
        }
    }
}