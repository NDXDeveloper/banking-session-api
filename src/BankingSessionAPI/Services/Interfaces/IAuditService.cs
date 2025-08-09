using BankingSessionAPI.Models.DTOs;
using BankingSessionAPI.Models.Entities;

namespace BankingSessionAPI.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId, string userId, string userName, 
        string? ipAddress, string? userAgent, string? sessionId, object? oldValues = null, 
        object? newValues = null, string? additionalInfo = null, bool isSuccessful = true, 
        string? errorMessage = null);
    
    Task LogLoginAsync(string userId, string userName, string ipAddress, string userAgent, 
        string sessionId, bool isSuccessful, string? errorMessage = null);
    
    Task LogLogoutAsync(string userId, string userName, string ipAddress, string? userAgent, string sessionId);
    
    Task LogPasswordChangeAsync(string userId, string userName, string ipAddress, string sessionId, 
        bool isSuccessful, string? errorMessage = null);
    
    Task LogUserActionAsync(string action, string userId, string userName, string ipAddress, 
        string? userAgent, string? sessionId, object? additionalData = null);
    
    Task LogSecurityEventAsync(string action, string userId, string userName, string ipAddress, 
        string? userAgent, string? sessionId, string details, string level = "Warning");
    
    Task LogSystemEventAsync(string action, string details, string level = "Information");
    
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(string? userId = null, string? action = null, 
        DateTime? startDate = null, DateTime? endDate = null, int skip = 0, int take = 50);
    
    Task<IEnumerable<AuditLogDto>> GetUserAuditLogsAsync(string userId, DateTime? startDate = null, 
        DateTime? endDate = null, int skip = 0, int take = 50);
    
    Task<IEnumerable<AuditLogDto>> GetSecurityAuditLogsAsync(DateTime? startDate = null, 
        DateTime? endDate = null, int skip = 0, int take = 50);
    
    Task<int> GetAuditLogsCountAsync(string? userId = null, string? action = null, 
        DateTime? startDate = null, DateTime? endDate = null);
    
    Task<Dictionary<string, int>> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    Task CleanupOldAuditLogsAsync(int retentionDays = 90);
    
    Task<bool> IsAuditingEnabledAsync();
    
    Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id);
}