namespace BankingSessionAPI.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    Task<bool> SendLoginNotificationAsync(string to, string userName, string ipAddress, string userAgent, DateTime loginTime);
    Task<bool> SendTwoFactorCodeAsync(string to, string userName, string code);
    Task<bool> SendPasswordResetAsync(string to, string userName, string resetToken);
    Task<bool> SendAccountLockoutAsync(string to, string userName, DateTime lockoutUntil);
    Task<bool> SendSuspiciousActivityAsync(string to, string userName, string activity, string ipAddress);
    Task<bool> SendSessionRevokedAsync(string to, string userName, string deviceName, string reason);
}