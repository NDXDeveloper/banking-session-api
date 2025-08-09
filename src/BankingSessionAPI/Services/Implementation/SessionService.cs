using BankingSessionAPI.Data;
using BankingSessionAPI.Models.DTOs;
using BankingSessionAPI.Models.Entities;
using BankingSessionAPI.Models.Requests;
using BankingSessionAPI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace BankingSessionAPI.Services.Implementation;

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISecurityService _securityService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<SessionService> _logger;
    private const int MaxConcurrentSessions = 5;

    public SessionService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ISecurityService securityService,
        IEmailService emailService,
        IMapper mapper,
        ILogger<SessionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _securityService = securityService;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LoginDto> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        try
        {
            // Vérifier le rate limiting
            var rateLimitKey = $"{ipAddress}:{request.Email}";
            if (await _securityService.IsRateLimitExceededAsync(rateLimitKey, "login"))
            {
                _logger.LogWarning("Rate limit dépassé pour {Email} depuis {IP}", request.Email, ipAddress);
                throw new InvalidOperationException("Trop de tentatives de connexion. Veuillez réessayer plus tard.");
            }

            await _securityService.RecordAttemptAsync(rateLimitKey, "login");

            // Trouver l'utilisateur
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Tentative de connexion avec email inexistant: {Email}", request.Email);
                return null!;
            }

            // Vérifier si le compte est verrouillé
            if (await _securityService.IsAccountLockedAsync(user.Id))
            {
                var lockoutTime = await _securityService.GetLockoutTimeRemainingAsync(user.Id);
                _logger.LogWarning("Tentative de connexion sur compte verrouillé: {UserId}", user.Id);
                throw new InvalidOperationException($"Compte verrouillé. Réessayez dans {lockoutTime?.TotalMinutes:F0} minutes.");
            }

            // Vérifier le mot de passe
            var passwordValid = await _securityService.VerifyPasswordAsync(request.Password, user.PasswordHash!);
            if (!passwordValid)
            {
                await _securityService.IncrementFailedLoginAttemptsAsync(user.Id);
                _logger.LogWarning("Mot de passe incorrect pour {UserId}", user.Id);
                return null!;
            }

            // Réinitialiser les tentatives échouées
            await _securityService.ResetFailedLoginAttemptsAsync(user.Id);

            // Vérifier si 2FA est activé
            var twoFactorEnabled = await _securityService.IsTwoFactorEnabledAsync(user.Id);
            if (twoFactorEnabled)
            {
                // Générer et envoyer le code 2FA
                var twoFactorCode = await _securityService.GenerateTwoFactorCodeAsync(user.Id);
                await _emailService.SendTwoFactorCodeAsync(user.Email!, user.UserName!, twoFactorCode);
                
                // Générer le token 2FA pour l'étape suivante
                var twoFactorTokenData = $"{user.Id}:{DateTime.UtcNow.Ticks}";
                var twoFactorToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(twoFactorTokenData));
                
                return new LoginDto
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = twoFactorToken,
                    Message = "Code de vérification envoyé par email."
                };
            }

            // Créer la session
            var sessionToken = await _securityService.GenerateSessionTokenAsync();
            
            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = sessionToken,
                DeviceId = request.DeviceId ?? Guid.NewGuid().ToString(),
                DeviceName = request.DeviceName,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.RememberMe ? 43200 : 480), // 30 jours ou 8 heures
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            // Envoyer notification de connexion
            await _emailService.SendLoginNotificationAsync(user.Email!, user.UserName!, ipAddress, userAgent, DateTime.UtcNow);

            _logger.LogInformation("Connexion réussie pour {UserId} depuis {IP}", user.Id, ipAddress);

            return new LoginDto
            {
                SessionToken = sessionToken,
                ExpiresAt = session.ExpiresAt,
                User = _mapper.Map<UserDto>(user),
                Session = _mapper.Map<SessionInfoDto>(session),
                RequiresTwoFactor = false,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion pour {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> LogoutAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive && !s.IsRevoked);

            if (session == null) return false;

            session.IsActive = false;
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "Déconnexion utilisateur";

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Déconnexion réussie pour session {SessionId}", session.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion pour session {SessionToken}", sessionToken);
            return false;
        }
    }

    public async Task<bool> LogoutAllSessionsAsync(string userId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && !s.IsRevoked)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;
                session.RevocationReason = "Déconnexion de toutes les sessions";
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Déconnexion de toutes les sessions pour utilisateur {UserId}. {Count} sessions fermées", userId, sessions.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion de toutes les sessions pour {UserId}", userId);
            return false;
        }
    }

    public async Task<SessionInfoDto?> GetSessionInfoAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session == null) return null;

            return _mapper.Map<SessionInfoDto>(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des informations de session {SessionToken}", sessionToken);
            return null;
        }
    }

    public async Task<IEnumerable<SessionInfoDto>> GetUserSessionsAsync(string userId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.IsActive && !s.IsRevoked)
                .OrderByDescending(s => s.LastAccessedAt ?? s.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SessionInfoDto>>(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des sessions pour {UserId}", userId);
            return Enumerable.Empty<SessionInfoDto>();
        }
    }

    public async Task<bool> RevokeSessionAsync(string sessionId, string revokedBy, string reason)
    {
        try
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return false;

            session.IsActive = false;
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedBy = revokedBy;
            session.RevocationReason = reason;

            await _context.SaveChangesAsync();
            
            // Envoyer notification
            await _emailService.SendSessionRevokedAsync(
                session.User.Email!, 
                session.User.UserName!, 
                session.DeviceName ?? "Appareil inconnu", 
                reason);
            
            _logger.LogInformation("Session {SessionId} révoquée par {RevokedBy}. Raison: {Reason}", sessionId, revokedBy, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la révocation de session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> RevokeUserSessionsAsync(string userId, string revokedBy, string reason)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.IsActive && !s.IsRevoked)
                .ToListAsync();

            if (!sessions.Any()) return false;

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedBy = revokedBy;
                session.RevocationReason = reason;
            }

            await _context.SaveChangesAsync();
            
            // Envoyer notification à l'utilisateur
            var user = sessions.First().User;
            await _emailService.SendSessionRevokedAsync(
                user.Email!, 
                user.UserName!, 
                "Toutes les sessions", 
                reason);
            
            _logger.LogInformation("Toutes les sessions de {UserId} révoquées par {RevokedBy}. {Count} sessions. Raison: {Reason}", 
                userId, revokedBy, sessions.Count, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la révocation des sessions pour {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session == null) return false;
            
            return session.IsValid; // Utilise la propriété IsValid de l'entité
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la validation de session {SessionToken}", sessionToken);
            return false;
        }
    }

    public async Task<bool> ExtendSessionAsync(string sessionToken, int additionalMinutes = 0)
    {
        try
        {
            // D'abord chercher la session sans filtre IsValid pour diagnostiquer
            var sessionCheck = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (sessionCheck == null)
            {
                _logger.LogWarning("Session complètement inexistante pour extension: {SessionToken}", sessionToken);
                return false;
            }

            // Log détaillé de l'état de la session
            _logger.LogInformation("État session pour extension - ID: {SessionId}, IsActive: {IsActive}, IsRevoked: {IsRevoked}, IsExpired: {IsExpired}, ExpiresAt: {ExpiresAt}", 
                sessionCheck.Id, sessionCheck.IsActive, sessionCheck.IsRevoked, sessionCheck.IsExpired, sessionCheck.ExpiresAt);

            // Ne pas utiliser IsValid à cause des problèmes de timezone
            // Vérifier manuellement les conditions avec UTC explicite
            var session = sessionCheck;
            var utcNow = DateTime.UtcNow;
            
            if (!session.IsActive || session.IsRevoked || session.ExpiresAt <= utcNow)
            {
                _logger.LogWarning("Session non valide pour extension: {SessionToken} - IsActive: {IsActive}, IsRevoked: {IsRevoked}, ExpiresAt: {ExpiresAt}, UtcNow: {UtcNow}", 
                    sessionToken, session.IsActive, session.IsRevoked, session.ExpiresAt, utcNow);
                return false;
            }

            // La vérification d'expiration est déjà faite ci-dessus avec UTC explicite

            // Extension par défaut de 30 minutes si pas spécifié
            var extensionMinutes = additionalMinutes > 0 ? additionalMinutes : 30;
            
            // Limiter l'extension (max 24h depuis maintenant)
            var newExpiration = session.ExpiresAt.AddMinutes(extensionMinutes);
            var maxAllowedExpiration = utcNow.AddHours(24);
            
            if (newExpiration > maxAllowedExpiration)
            {
                _logger.LogWarning("Extension refusée - Dépasse la limite de 24h. Session: {SessionId}, Demandé: {RequestedExpiration}, Max: {MaxExpiration}", 
                    session.Id, newExpiration, maxAllowedExpiration);
                return false;
            }

            // Limiter les extensions trop importantes (max 8h d'un coup)
            if (extensionMinutes > 480) // 8 heures
            {
                _logger.LogWarning("Extension refusée - Trop importante ({Minutes} minutes). Session: {SessionId}", 
                    extensionMinutes, session.Id);
                return false;
            }

            session.ExpiresAt = newExpiration;
            session.LastAccessedAt = utcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Session {SessionId} étendue de {Minutes} minutes", session.Id, extensionMinutes);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'extension de session {SessionToken}", sessionToken);
            return false;
        }
    }

    public async Task<UserSession?> GetSessionByTokenAsync(string sessionToken)
    {
        try
        {
            return await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de session par token {SessionToken}", sessionToken);
            return null;
        }
    }

    public async Task<bool> IsSessionActiveAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            return session?.IsValid ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification d'activité de session {SessionToken}", sessionToken);
            return false;
        }
    }

    public async Task<int> GetActiveSessionsCountAsync(string userId)
    {
        try
        {
            return await _context.UserSessions
                .CountAsync(s => s.UserId == userId && s.IsActive && !s.IsRevoked && !s.IsExpired);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du comptage des sessions actives pour {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> HasMaxConcurrentSessionsAsync(string userId)
    {
        try
        {
            var activeSessionsCount = await GetActiveSessionsCountAsync(userId);
            return activeSessionsCount >= MaxConcurrentSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification du nombre max de sessions pour {UserId}", userId);
            return false;
        }
    }

    public async Task UpdateSessionActivityAsync(string sessionToken, string? ipAddress = null)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsValid);

            if (session != null)
            {
                session.LastAccessedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(ipAddress))
                    session.IpAddress = ipAddress;

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour d'activité de session {SessionToken}", sessionToken);
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;
                session.RevocationReason = "Session expirée automatiquement";
            }

            if (expiredSessions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("{Count} sessions expirées nettoyées", expiredSessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du nettoyage des sessions expirées");
        }
    }

    public async Task<bool> CreateSessionAsync(ApplicationUser user, string deviceId, string? deviceName, string ipAddress, string userAgent, bool rememberMe = false)
    {
        var token = await CreateSessionWithTokenAsync(user, deviceId, deviceName, ipAddress, userAgent, rememberMe);
        return token != null;
    }

    public async Task<string?> CreateSessionWithTokenAsync(ApplicationUser user, string deviceId, string? deviceName, string ipAddress, string userAgent, bool rememberMe = false)
    {
        try
        {
            // Vérifier le nombre de sessions concurrentes
            if (await HasMaxConcurrentSessionsAsync(user.Id))
            {
                // Supprimer la plus ancienne session
                var oldestSession = await _context.UserSessions
                    .Where(s => s.UserId == user.Id && s.IsActive && !s.IsRevoked)
                    .OrderBy(s => s.LastAccessedAt ?? s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (oldestSession != null)
                {
                    oldestSession.IsActive = false;
                    oldestSession.IsRevoked = true;
                    oldestSession.RevokedAt = DateTime.UtcNow;
                    oldestSession.RevocationReason = "Limite de sessions concurrentes atteinte";
                }
            }

            var sessionToken = await _securityService.GenerateSessionTokenAsync();
            
            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = sessionToken,
                DeviceId = deviceId,
                DeviceName = deviceName,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddMinutes(rememberMe ? 43200 : 480), // 30 jours ou 8 heures
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Session créée pour utilisateur {UserId} avec device {DeviceId}, token {Token}", user.Id, deviceId, sessionToken.Substring(0, 8) + "...");
            return sessionToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de session pour {UserId}", user.Id);
            return null;
        }
    }

    public async Task<SessionInfoDto?> GetCurrentSessionAsync(string userId, string deviceId)
    {
        try
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.DeviceId == deviceId && s.IsValid)
                .OrderByDescending(s => s.LastAccessedAt ?? s.CreatedAt)
                .FirstOrDefaultAsync();

            return session != null ? _mapper.Map<SessionInfoDto>(session) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la session courante pour {UserId} device {DeviceId}", userId, deviceId);
            return null;
        }
    }

    public async Task<bool> TerminateSessionsByDeviceAsync(string userId, string deviceId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.DeviceId == deviceId && s.IsActive && !s.IsRevoked)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;
                session.RevocationReason = "Terminé par appareil";
            }

            if (sessions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("{Count} sessions terminées pour {UserId} device {DeviceId}", sessions.Count, userId, deviceId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la terminaison des sessions pour {UserId} device {DeviceId}", userId, deviceId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetSessionStatisticsAsync(string userId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var stats = new Dictionary<string, object>();

            // Sessions actives
            var activeSessions = await _context.UserSessions
                .CountAsync(s => s.UserId == userId && s.IsValid);

            // Sessions créées aujourd'hui
            var todaySessions = await _context.UserSessions
                .CountAsync(s => s.UserId == userId && s.CreatedAt.Date == now.Date);

            // Sessions créées cette semaine
            var weekStart = now.AddDays(-(int)now.DayOfWeek);
            var weekSessions = await _context.UserSessions
                .CountAsync(s => s.UserId == userId && s.CreatedAt >= weekStart);

            // Appareils uniques
            var uniqueDevices = await _context.UserSessions
                .Where(s => s.UserId == userId)
                .Select(s => s.DeviceId)
                .Distinct()
                .CountAsync();

            // Dernière connexion
            var lastLogin = await _context.UserSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            stats["activeSessions"] = activeSessions;
            stats["todaySessions"] = todaySessions;
            stats["weekSessions"] = weekSessions;
            stats["uniqueDevices"] = uniqueDevices;
            stats["lastLogin"] = lastLogin;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des statistiques pour {UserId}", userId);
            return new Dictionary<string, object>();
        }
    }
}