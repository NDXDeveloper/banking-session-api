namespace BankingSessionAPI.Constants;

public static class SecurityConstants
{
    // Headers de sécurité
    public const string SessionTokenHeader = "X-Session-Token";
    public const string DeviceIdHeader = "X-Device-Id";
    public const string RequestIdHeader = "X-Request-Id";
    public const string UserAgentHeader = "User-Agent";
    public const string ForwardedForHeader = "X-Forwarded-For";
    public const string RealIpHeader = "X-Real-IP";

    // Cookies
    public const string SessionCookieName = "banking-session";
    public const string DeviceCookieName = "banking-device";
    public const string AntiForgeryCookieName = "banking-antiforgery";

    // Claims personnalisés
    public const string SessionIdClaim = "session_id";
    public const string DeviceIdClaim = "device_id";
    public const string LastLoginClaim = "last_login";
    public const string UserRoleClaim = "user_role";
    public const string UserIdClaim = "user_id";
    public const string FullNameClaim = "full_name";

    // Politiques d'autorisation
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireManagerRole = "RequireManagerRole";
    public const string RequireUserRole = "RequireUserRole";
    public const string RequireActiveSession = "RequireActiveSession";
    public const string RequireTwoFactor = "RequireTwoFactor";

    // Types de contenus sécurisés
    public static readonly string[] AllowedContentTypes = 
    {
        "application/json",
        "application/x-www-form-urlencoded",
        "multipart/form-data"
    };

    // Headers de sécurité requis
    public static readonly Dictionary<string, string> SecurityHeaders = new()
    {
        { "X-Content-Type-Options", "nosniff" },
        { "X-Frame-Options", "DENY" },
        { "X-XSS-Protection", "1; mode=block" },
        { "Referrer-Policy", "strict-origin-when-cross-origin" },
        { "Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self';" },
        { "Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()" }
    };

    // Tailles limites
    public const int MaxRequestBodySize = 1024 * 1024; // 1MB
    public const int MaxHeaderValueLength = 1024;
    public const int MaxUrlLength = 2048;
    public const int MaxUserAgentLength = 500;

    // Durées de sécurité
    public const int DefaultSessionTimeoutMinutes = 30;
    public const int MaxSessionTimeoutMinutes = 480; // 8 heures
    public const int DefaultLockoutMinutes = 15;
    public const int MaxFailedLoginAttempts = 5;
    public const int PasswordHistoryLimit = 5;

    // Expressions régulières de validation
    public const string EmailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    public const string PhoneRegex = @"^\+?[1-9]\d{1,14}$";
    public const string StrongPasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$";
    public const string SessionTokenRegex = @"^[A-Za-z0-9+/]+=*$";
    public const string DeviceIdRegex = @"^[a-zA-Z0-9\-_]{10,50}$";
}