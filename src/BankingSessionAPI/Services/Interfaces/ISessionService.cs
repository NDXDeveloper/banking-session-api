using BankingSessionAPI.Models.DTOs;
using BankingSessionAPI.Models.Entities;
using BankingSessionAPI.Models.Requests;

namespace BankingSessionAPI.Services.Interfaces;

public interface ISessionService
{
    Task<LoginDto> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<bool> LogoutAsync(string sessionToken);
    Task<bool> LogoutAllSessionsAsync(string userId);
    Task<SessionInfoDto?> GetSessionInfoAsync(string sessionToken);
    Task<IEnumerable<SessionInfoDto>> GetUserSessionsAsync(string userId);
    Task<bool> RevokeSessionAsync(string sessionId, string revokedBy, string reason);
    Task<bool> RevokeUserSessionsAsync(string userId, string revokedBy, string reason);
    Task<bool> ValidateSessionAsync(string sessionToken);
    Task<bool> ExtendSessionAsync(string sessionToken, int additionalMinutes = 0);
    Task<UserSession?> GetSessionByTokenAsync(string sessionToken);
    Task<bool> IsSessionActiveAsync(string sessionToken);
    Task<int> GetActiveSessionsCountAsync(string userId);
    Task<bool> HasMaxConcurrentSessionsAsync(string userId);
    Task UpdateSessionActivityAsync(string sessionToken, string? ipAddress = null);
    Task CleanupExpiredSessionsAsync();
    Task<bool> CreateSessionAsync(ApplicationUser user, string deviceId, string? deviceName, string ipAddress, string userAgent, bool rememberMe = false);
    Task<string?> CreateSessionWithTokenAsync(ApplicationUser user, string deviceId, string? deviceName, string ipAddress, string userAgent, bool rememberMe = false);
    Task<SessionInfoDto?> GetCurrentSessionAsync(string userId, string deviceId);
    Task<bool> TerminateSessionsByDeviceAsync(string userId, string deviceId);
    Task<Dictionary<string, object>> GetSessionStatisticsAsync(string userId);
}