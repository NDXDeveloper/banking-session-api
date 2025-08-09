namespace BankingSessionAPI.Services.Interfaces;

public interface ISecurityService
{
    Task<string> GenerateSessionTokenAsync();
    Task<string> GenerateRefreshTokenAsync();
    Task<bool> ValidatePasswordStrengthAsync(string password);
    Task<bool> IsPasswordInHistoryAsync(string userId, string password);
    Task AddPasswordToHistoryAsync(string userId, string passwordHash);
    Task<bool> IsAccountLockedAsync(string userId);
    Task<TimeSpan?> GetLockoutTimeRemainingAsync(string userId);
    Task IncrementFailedLoginAttemptsAsync(string userId);
    Task ResetFailedLoginAttemptsAsync(string userId);
    Task LockAccountAsync(string userId, TimeSpan lockoutDuration);
    Task UnlockAccountAsync(string userId);
    Task<bool> IsValidIpAddressAsync(string ipAddress);
    Task<bool> IsValidUserAgentAsync(string userAgent);
    Task<bool> IsSuspiciousActivityAsync(string userId, string ipAddress, string userAgent);
    Task<bool> IsRateLimitExceededAsync(string identifier, string action);
    Task RecordAttemptAsync(string identifier, string action);
    Task<string> HashPasswordAsync(string password);
    Task<bool> VerifyPasswordAsync(string password, string hashedPassword);
    Task<string> EncryptSensitiveDataAsync(string data);
    Task<string> DecryptSensitiveDataAsync(string encryptedData);
    Task<bool> ValidateRequestIntegrityAsync(string requestBody, string signature);
    Task<string> GenerateRequestSignatureAsync(string requestBody);
    Task<bool> IsValidDeviceIdAsync(string deviceId);
    Task<bool> IsValidSessionTokenFormatAsync(string token);
    Task<Dictionary<string, object>> GetSecurityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> ValidateTwoFactorCodeAsync(string userId, string code);
    Task<string> GenerateTwoFactorCodeAsync(string userId);
    Task<bool> IsTwoFactorEnabledAsync(string userId);
    Task EnableTwoFactorAsync(string userId);
    Task DisableTwoFactorAsync(string userId);
    Task<List<string>> GetTrustedDevicesAsync(string userId);
    Task AddTrustedDeviceAsync(string userId, string deviceId);
    Task RemoveTrustedDeviceAsync(string userId, string deviceId);
    Task<bool> IsTrustedDeviceAsync(string userId, string deviceId);
}