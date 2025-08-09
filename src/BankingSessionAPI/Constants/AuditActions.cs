namespace BankingSessionAPI.Constants;

public static class AuditActions
{
    // Actions d'authentification
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
    public const string PasswordResetCompleted = "PASSWORD_RESET_COMPLETED";
    public const string TwoFactorEnabled = "TWO_FACTOR_ENABLED";
    public const string TwoFactorDisabled = "TWO_FACTOR_DISABLED";

    // Actions sur les utilisateurs
    public const string UserCreated = "USER_CREATED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserDeleted = "USER_DELETED";
    public const string UserActivated = "USER_ACTIVATED";
    public const string UserDeactivated = "USER_DEACTIVATED";
    public const string UserLocked = "USER_LOCKED";
    public const string UserUnlocked = "USER_UNLOCKED";
    public const string UserRoleAdded = "USER_ROLE_ADDED";
    public const string UserRoleRemoved = "USER_ROLE_REMOVED";

    // Actions sur les comptes
    public const string AccountCreated = "ACCOUNT_CREATED";
    public const string AccountUpdated = "ACCOUNT_UPDATED";
    public const string AccountClosed = "ACCOUNT_CLOSED";
    public const string AccountReopened = "ACCOUNT_REOPENED";
    public const string BalanceInquiry = "BALANCE_INQUIRY";
    public const string TransactionPerformed = "TRANSACTION_PERFORMED";

    // Actions de sécurité
    public const string UnauthorizedAccess = "UNAUTHORIZED_ACCESS";
    public const string SuspiciousActivity = "SUSPICIOUS_ACTIVITY";
    public const string SecurityBreach = "SECURITY_BREACH";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string InvalidTokenUsage = "INVALID_TOKEN_USAGE";

    // Actions administratives
    public const string ConfigurationChanged = "CONFIGURATION_CHANGED";
    public const string SystemMaintenance = "SYSTEM_MAINTENANCE";
    public const string DataExported = "DATA_EXPORTED";
    public const string DataImported = "DATA_IMPORTED";
    public const string BackupCreated = "BACKUP_CREATED";
    public const string BackupRestored = "BACKUP_RESTORED";

    // Actions sur les sessions
    public const string SessionCreated = "SESSION_CREATED";
    public const string SessionUpdated = "SESSION_UPDATED";
    public const string SessionTerminated = "SESSION_TERMINATED";
    public const string ConcurrentSessionDetected = "CONCURRENT_SESSION_DETECTED";
    public const string SessionCleanup = "SESSION_CLEANUP";
}