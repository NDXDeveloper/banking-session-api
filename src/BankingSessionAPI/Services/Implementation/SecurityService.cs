using BankingSessionAPI.Services.Interfaces;
using BankingSessionAPI.Models.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace BankingSessionAPI.Services.Implementation;

public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private readonly PasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, List<DateTime>> _rateLimitAttempts = new();
    private readonly Dictionary<string, int> _failedAttempts = new();
    private readonly Dictionary<string, DateTime> _lockoutTimes = new();
    private readonly Dictionary<string, List<string>> _passwordHistory = new();
    private readonly Dictionary<string, string> _twoFactorCodes = new();
    private readonly Dictionary<string, DateTime> _twoFactorExpiry = new();

    public SecurityService(ILogger<SecurityService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _passwordHasher = new PasswordHasher<ApplicationUser>();
        _serviceProvider = serviceProvider;
    }

    public Task<string> GenerateSessionTokenAsync()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Task.FromResult(Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", ""));
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[64];
        rng.GetBytes(bytes);
        return Task.FromResult(Convert.ToBase64String(bytes));
    }

    public Task<bool> ValidatePasswordStrengthAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Task.FromResult(false);

        // Au moins 8 caractères
        if (password.Length < 8)
            return Task.FromResult(false);

        // Au moins une majuscule
        if (!password.Any(char.IsUpper))
            return Task.FromResult(false);

        // Au moins une minuscule
        if (!password.Any(char.IsLower))
            return Task.FromResult(false);

        // Au moins un chiffre
        if (!password.Any(char.IsDigit))
            return Task.FromResult(false);

        // Au moins un caractère spécial
        var specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        if (!password.Any(specialChars.Contains))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
    public Task<bool> IsPasswordInHistoryAsync(string userId, string password)
    {
        if (!_passwordHistory.ContainsKey(userId))
            return Task.FromResult(false);

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var history = _passwordHistory[userId];
        
        return Task.FromResult(history.Any(h => BCrypt.Net.BCrypt.Verify(password, h)));
    }
    public Task AddPasswordToHistoryAsync(string userId, string passwordHash)
    {
        if (!_passwordHistory.ContainsKey(userId))
            _passwordHistory[userId] = new List<string>();

        _passwordHistory[userId].Add(passwordHash);
        
        // Garder seulement les 5 derniers mots de passe
        if (_passwordHistory[userId].Count > 5)
            _passwordHistory[userId].RemoveAt(0);

        return Task.CompletedTask;
    }
    public Task<bool> IsAccountLockedAsync(string userId)
    {
        if (!_lockoutTimes.ContainsKey(userId))
            return Task.FromResult(false);

        var lockoutTime = _lockoutTimes[userId];
        var isLocked = DateTime.UtcNow < lockoutTime;
        
        if (!isLocked)
        {
            _lockoutTimes.Remove(userId);
            _failedAttempts.Remove(userId);
        }

        return Task.FromResult(isLocked);
    }
    public Task<TimeSpan?> GetLockoutTimeRemainingAsync(string userId)
    {
        if (!_lockoutTimes.ContainsKey(userId))
            return Task.FromResult<TimeSpan?>(null);

        var lockoutTime = _lockoutTimes[userId];
        var remaining = lockoutTime - DateTime.UtcNow;
        
        return Task.FromResult<TimeSpan?>(remaining > TimeSpan.Zero ? remaining : null);
    }
    public Task IncrementFailedLoginAttemptsAsync(string userId)
    {
        if (!_failedAttempts.ContainsKey(userId))
            _failedAttempts[userId] = 0;

        _failedAttempts[userId]++;
        
        // Verrouiller après 5 tentatives échouées
        if (_failedAttempts[userId] >= 5)
        {
            var lockoutDuration = TimeSpan.FromMinutes(30); // 30 minutes
            _lockoutTimes[userId] = DateTime.UtcNow.Add(lockoutDuration);
            _logger.LogWarning("Compte {UserId} verrouillé pour {Duration} minutes après {Attempts} tentatives échouées", 
                userId, lockoutDuration.TotalMinutes, _failedAttempts[userId]);
        }

        return Task.CompletedTask;
    }
    public Task ResetFailedLoginAttemptsAsync(string userId)
    {
        _failedAttempts.Remove(userId);
        _lockoutTimes.Remove(userId);
        return Task.CompletedTask;
    }
    public Task LockAccountAsync(string userId, TimeSpan lockoutDuration)
    {
        _lockoutTimes[userId] = DateTime.UtcNow.Add(lockoutDuration);
        _logger.LogWarning("Compte {UserId} verrouillé manuellement pour {Duration} minutes", userId, lockoutDuration.TotalMinutes);
        return Task.CompletedTask;
    }
    public Task UnlockAccountAsync(string userId)
    {
        _lockoutTimes.Remove(userId);
        _failedAttempts.Remove(userId);
        _logger.LogInformation("Compte {UserId} déverrouillé manuellement", userId);
        return Task.CompletedTask;
    }
    public Task<bool> IsValidIpAddressAsync(string ipAddress) => Task.FromResult(true);
    public Task<bool> IsValidUserAgentAsync(string userAgent) => Task.FromResult(true);
    public Task<bool> IsSuspiciousActivityAsync(string userId, string ipAddress, string userAgent) => Task.FromResult(false);
    public Task<bool> IsRateLimitExceededAsync(string identifier, string action)
    {
        var key = $"{identifier}:{action}";
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-5); // Fenêtre de 5 minutes
        
        if (!_rateLimitAttempts.ContainsKey(key))
            _rateLimitAttempts[key] = new List<DateTime>();

        // Nettoyer les anciennes tentatives
        _rateLimitAttempts[key] = _rateLimitAttempts[key].Where(t => t > windowStart).ToList();
        
        // Limites par action
        var limit = action.ToLower() switch
        {
            "login" => 10, // 10 tentatives de login par 5 minutes
            "password_reset" => 3, // 3 demandes de reset par 5 minutes
            "two_factor" => 5, // 5 tentatives 2FA par 5 minutes
            _ => 20 // Limite générale
        };

        return Task.FromResult(_rateLimitAttempts[key].Count >= limit);
    }
    public Task RecordAttemptAsync(string identifier, string action)
    {
        var key = $"{identifier}:{action}";
        
        if (!_rateLimitAttempts.ContainsKey(key))
            _rateLimitAttempts[key] = new List<DateTime>();

        _rateLimitAttempts[key].Add(DateTime.UtcNow);
        return Task.CompletedTask;
    }
    public Task<string> HashPasswordAsync(string password) 
    {
        var user = new ApplicationUser(); // Dummy user for hashing
        return Task.FromResult(_passwordHasher.HashPassword(user, password));
    }
    
    public Task<bool> VerifyPasswordAsync(string password, string hashedPassword) 
    {
        var user = new ApplicationUser(); // Dummy user for verification
        var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, password);
        return Task.FromResult(result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded);
    }
    public Task<string> EncryptSensitiveDataAsync(string data) => Task.FromResult(data);
    public Task<string> DecryptSensitiveDataAsync(string encryptedData) => Task.FromResult(encryptedData);
    public Task<bool> ValidateRequestIntegrityAsync(string requestBody, string signature) => Task.FromResult(true);
    public Task<string> GenerateRequestSignatureAsync(string requestBody) => Task.FromResult(string.Empty);
    public Task<bool> IsValidDeviceIdAsync(string deviceId) => Task.FromResult(true);
    public Task<bool> IsValidSessionTokenFormatAsync(string token) => Task.FromResult(true);
    public Task<Dictionary<string, object>> GetSecurityMetricsAsync(DateTime? startDate = null, DateTime? endDate = null) => Task.FromResult(new Dictionary<string, object>());
    public Task<bool> ValidateTwoFactorCodeAsync(string userId, string code)
    {
        if (!_twoFactorCodes.ContainsKey(userId) || !_twoFactorExpiry.ContainsKey(userId))
            return Task.FromResult(false);

        // Vérifier l'expiration
        if (DateTime.UtcNow > _twoFactorExpiry[userId])
        {
            _twoFactorCodes.Remove(userId);
            _twoFactorExpiry.Remove(userId);
            return Task.FromResult(false);
        }

        var isValid = _twoFactorCodes[userId] == code;
        
        if (isValid)
        {
            // Nettoyer après validation réussie
            _twoFactorCodes.Remove(userId);
            _twoFactorExpiry.Remove(userId);
            _logger.LogInformation("Code 2FA validé avec succès pour l'utilisateur {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("Code 2FA invalide pour l'utilisateur {UserId}", userId);
        }

        return Task.FromResult(isValid);
    }
    public Task<string> GenerateTwoFactorCodeAsync(string userId)
    {
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        
        _twoFactorCodes[userId] = code;
        _twoFactorExpiry[userId] = DateTime.UtcNow.AddMinutes(5); // Expire dans 5 minutes
        
        _logger.LogInformation("Code 2FA généré pour l'utilisateur {UserId}", userId);
        return Task.FromResult(code);
    }
    public async Task<bool> IsTwoFactorEnabledAsync(string userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        return user?.TwoFactorEnabled ?? false;
    }
    public Task EnableTwoFactorAsync(string userId) => Task.CompletedTask;
    public Task DisableTwoFactorAsync(string userId) => Task.CompletedTask;
    public Task<List<string>> GetTrustedDevicesAsync(string userId) => Task.FromResult(new List<string>());
    public Task AddTrustedDeviceAsync(string userId, string deviceId) => Task.CompletedTask;
    public Task RemoveTrustedDeviceAsync(string userId, string deviceId) => Task.CompletedTask;
    public Task<bool> IsTrustedDeviceAsync(string userId, string deviceId) => Task.FromResult(false);
}