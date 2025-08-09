using BankingSessionAPI.Constants;
using BankingSessionAPI.Models.DTOs;
using BankingSessionAPI.Models.Requests;
using BankingSessionAPI.Models.Responses;
using BankingSessionAPI.Services.Interfaces;
using BankingSessionAPI.Models.Entities;
using BankingSessionAPI.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AutoMapper;

using System.Security.Claims;
using System.Text;

namespace BankingSessionAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionAuthController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly ISecurityService _securityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SessionAuthController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly SecuritySettings _securitySettings;

    public SessionAuthController(
        ISessionService sessionService,
        IAuditService auditService,
        ISecurityService securityService,
        IEmailService emailService,
        ILogger<SessionAuthController> logger,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        IOptions<SecuritySettings> securitySettings)
    {
        _sessionService = sessionService;
        _auditService = auditService;
        _securityService = securityService;
        _emailService = emailService;
        _logger = logger;
        _userManager = userManager;
        _mapper = mapper;
        _securitySettings = securitySettings.Value;
    }

    /// <summary>
    /// Authentifie un utilisateur et crée une nouvelle session
    /// </summary>
    /// <param name="request">Informations de connexion</param>
    /// <returns>Token d'authentification et informations de session</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _sessionService.LoginAsync(request, ipAddress, userAgent);
            
            if (result == null)
            {
                await _auditService.LogLoginAsync(
                    "Unknown", 
                    request.Email, 
                    ipAddress, 
                    userAgent, 
                    "Failed-Login", 
                    false, 
                    "Échec de l'authentification");

                return Unauthorized(new { message = "Identifiants invalides" });
            }

            _logger.LogInformation("Login result: SessionToken={SessionToken}, Success={Success}, RequiresTwoFactor={RequiresTwoFactor}, User={User}, Session={Session}", 
                result.SessionToken, result.Success, result.RequiresTwoFactor, result.User?.Email, result.Session?.Id);

            // Pour le 2FA, nous n'avons pas besoin de l'objet user complet, juste créer la réponse sécurisée
            var responseUser = result.RequiresTwoFactor ? null : 
                (result.User != null ? await _userManager.FindByEmailAsync(request.Email) : null);
            
            return Ok(CreateLoginResponse(result, responseUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion pour {Email}", request.Email);
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Déconnecte l'utilisateur et révoque la session active
    /// </summary>
    /// <returns>Confirmation de déconnexion</returns>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "SessionScheme")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionToken = GetSessionToken();
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized(new { message = "Token de session manquant" });
            }

            var result = await _sessionService.LogoutAsync(sessionToken);
            
            if (!result)
            {
                return BadRequest(new { message = "Échec de la déconnexion" });
            }

            await _auditService.LogLogoutAsync(
                User.FindFirst(SecurityConstants.UserIdClaim)?.Value ?? string.Empty,
                User.Identity?.Name ?? string.Empty,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                sessionToken);

            // Supprimer le cookie de session si utilisé
            if (ShouldUseCookies())
            {
                ClearSessionCookie();
            }

            return Ok(new { message = "Déconnexion réussie" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Récupère les informations de la session active
    /// </summary>
    /// <returns>Informations de session</returns>
    [HttpGet("session-info")]
    [Authorize(AuthenticationSchemes = "SessionScheme")]
    [ProducesResponseType(typeof(SessionInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionInfo()
    {
        try
        {
            var sessionToken = GetSessionToken();
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized(new { message = "Token de session manquant" });
            }

            var sessionInfo = await _sessionService.GetSessionInfoAsync(sessionToken);
            
            if (sessionInfo == null)
            {
                return NotFound(new { message = "Session non trouvée" });
            }

            return Ok(sessionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des informations de session");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Récupère toutes les sessions actives de l'utilisateur
    /// </summary>
    /// <returns>Liste des sessions utilisateur</returns>
    [HttpGet("user-sessions")]
    [Authorize(AuthenticationSchemes = "SessionScheme")]
    [ProducesResponseType(typeof(IEnumerable<SessionInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserSessions()
    {
        try
        {
            var userId = User.FindFirst(SecurityConstants.UserIdClaim)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Utilisateur non identifié" });
            }

            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des sessions utilisateur");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Révoque une session spécifique (Admin uniquement)
    /// </summary>
    /// <param name="sessionId">ID de la session à révoquer</param>
    /// <param name="reason">Raison de la révocation</param>
    /// <returns>Confirmation de révocation</returns>
    [HttpPost("revoke-session/{sessionId}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeSession(string sessionId, [FromBody] string reason = "Révoquée par l'administrateur")
    {
        try
        {
            var revokedBy = User.Identity?.Name ?? "Admin";
            var result = await _sessionService.RevokeSessionAsync(sessionId, revokedBy, reason);
            
            if (!result)
            {
                return BadRequest(new { message = "Échec de la révocation de session" });
            }

            await _auditService.LogSecurityEventAsync(
                AuditActions.SessionRevoked,
                User.FindFirst(SecurityConstants.UserIdClaim)?.Value ?? string.Empty,
                revokedBy,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                GetSessionToken(),
                $"Session {sessionId} révoquée. Raison: {reason}");

            return Ok(new { message = "Session révoquée avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la révocation de session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Prolonge la session active
    /// </summary>
    /// <param name="additionalMinutes">Minutes supplémentaires (optionnel)</param>
    /// <returns>Confirmation de prolongation</returns>
    [HttpPost("extend-session")]
    [Authorize(AuthenticationSchemes = "SessionScheme")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExtendSession([FromBody] int additionalMinutes = 0)
    {
        try
        {
            var sessionToken = GetSessionToken();
            if (string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized(new { message = "Token de session manquant" });
            }

            var result = await _sessionService.ExtendSessionAsync(sessionToken, additionalMinutes);
            
            if (!result)
            {
                // Audit de l'échec de prolongation
                await _auditService.LogUserActionAsync(
                    AuditActions.SessionUpdated,
                    User.FindFirst(SecurityConstants.UserIdClaim)?.Value ?? "Unknown",
                    User.Identity?.Name ?? "Unknown",
                    GetClientIpAddress(),
                    Request.Headers.UserAgent.ToString(),
                    sessionToken,
                    new { 
                        AdditionalMinutes = additionalMinutes, 
                        Success = false,
                        Error = "Impossible de prolonger la session"
                    });
                
                return BadRequest(new { message = "Impossible de prolonger la session" });
            }

            // Audit du succès de prolongation
            await _auditService.LogUserActionAsync(
                AuditActions.SessionUpdated,
                User.FindFirst(SecurityConstants.UserIdClaim)?.Value ?? "Unknown", 
                User.Identity?.Name ?? "Unknown",
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                sessionToken,
                new { 
                    AdditionalMinutes = additionalMinutes, 
                    Success = true,
                    ExtendedFrom = GetClientIpAddress()
                });

            return Ok(new { message = "Session prolongée avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la prolongation de session");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Révoque toutes les sessions d'un utilisateur
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <param name="reason">Raison de la révocation</param>
    /// <returns>Confirmation de révocation</returns>
    [HttpPost("revoke-user-sessions/{userId}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeUserSessions(string userId, [FromBody] string reason = "Révoquées par l'administrateur")
    {
        try
        {
            var revokedBy = User.Identity?.Name ?? "Admin";
            var result = await _sessionService.RevokeUserSessionsAsync(userId, revokedBy, reason);
            
            if (!result)
            {
                return BadRequest(new { message = "Échec de la révocation des sessions utilisateur" });
            }

            await _auditService.LogSecurityEventAsync(
                AuditActions.SessionRevoked,
                User.FindFirst(SecurityConstants.UserIdClaim)?.Value ?? string.Empty,
                revokedBy,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                GetSessionToken(),
                $"Toutes les sessions de l'utilisateur {userId} révoquées. Raison: {reason}");

            return Ok(new { message = "Sessions utilisateur révoquées avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la révocation des sessions utilisateur {UserId}", userId);
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Vérifie le code à deux facteurs et finalise la connexion
    /// </summary>
    /// <param name="request">Informations de vérification 2FA</param>
    /// <returns>Token d'authentification et informations de session</returns>
    [HttpPost("verify-2fa")]
    [ProducesResponseType(typeof(LoginDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorRequest request)
    {
        try
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            // Décoder le token 2FA pour récupérer l'userId
            string userId;
            try
            {
                var tokenData = Encoding.UTF8.GetString(Convert.FromBase64String(request.TwoFactorToken));
                var parts = tokenData.Split(':');
                if (parts.Length != 2)
                    return BadRequest(new { message = "Token 2FA invalide" });
                
                userId = parts[0];
                var timestamp = long.Parse(parts[1]);
                
                // Vérifier que le token n'a pas expiré (10 minutes max)
                var tokenTime = new DateTime(timestamp);
                if (DateTime.UtcNow - tokenTime > TimeSpan.FromMinutes(10))
                    return BadRequest(new { message = "Token 2FA expiré" });
            }
            catch
            {
                return BadRequest(new { message = "Token 2FA invalide" });
            }

            // Vérifier le rate limiting
            var rateLimitKey = $"{ipAddress}:{userId}";
            if (await _securityService.IsRateLimitExceededAsync(rateLimitKey, "two_factor"))
            {
                _logger.LogWarning("Rate limit dépassé pour vérification 2FA userId {UserId} depuis {IP}", userId, ipAddress);
                return StatusCode(429, new { message = "Trop de tentatives. Veuillez réessayer plus tard." });
            }

            await _securityService.RecordAttemptAsync(rateLimitKey, "two_factor");

            // Valider le code 2FA
            var isValidCode = await _securityService.ValidateTwoFactorCodeAsync(userId, request.Code);
            if (!isValidCode)
            {
                _logger.LogWarning("Code 2FA invalide pour userId {UserId}", userId);
                
                // Audit de l'échec de vérification 2FA
                await _auditService.LogSecurityEventAsync(
                    AuditActions.TwoFactorEnabled, // Utilisation du 2FA
                    userId,
                    "Unknown", // Nom d'utilisateur pas encore récupéré
                    ipAddress,
                    userAgent,
                    request.TwoFactorToken,
                    $"Échec de vérification 2FA depuis {ipAddress}",
                    "Warning");
                
                return Unauthorized(new { message = "Code de vérification invalide" });
            }

            // Récupérer l'utilisateur depuis la base de données
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("Utilisateur {UserId} non trouvé lors de la vérification 2FA", userId);
                return BadRequest(new { message = "Utilisateur non trouvé" });
            }
            
            // S'assurer d'avoir un deviceId cohérent
            var deviceId = request.DeviceId ?? Guid.NewGuid().ToString();
            
            // Créer la session et récupérer le token
            var sessionToken = await _sessionService.CreateSessionWithTokenAsync(
                user, 
                deviceId, 
                request.DeviceName, 
                ipAddress, 
                userAgent, 
                request.RememberMe);

            if (string.IsNullOrEmpty(sessionToken))
            {
                _logger.LogError("Échec de création de session pour {UserId}", userId);
                return StatusCode(500, new { message = "Erreur lors de la création de session" });
            }

            // Récupérer la session créée pour avoir les infos complètes
            var sessionInfo = await _sessionService.GetCurrentSessionAsync(userId, deviceId);
            var expiresAt = DateTime.UtcNow.AddMinutes(request.RememberMe ? 43200 : 480);
            
            _logger.LogInformation("Connexion 2FA réussie pour utilisateur {UserId} depuis {IP}", userId, ipAddress);

            // Audit spécifique de vérification 2FA réussie  
            await _auditService.LogSecurityEventAsync(
                AuditActions.TwoFactorEnabled,
                user.Id,
                user.Email!,
                ipAddress,
                userAgent,
                sessionToken,
                $"Vérification 2FA réussie depuis {ipAddress}",
                "Information");

            // Enregistrer l'audit de connexion
            await _auditService.LogLoginAsync(
                user.Id,
                user.Email!,
                ipAddress,
                userAgent,
                sessionToken,
                true,
                "Connexion 2FA réussie");

            var loginResponse = new LoginDto
            {
                SessionToken = sessionToken,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserDto>(user),
                Session = sessionInfo,
                RequiresTwoFactor = false,
                Message = "Connexion réussie",
                Success = true
            };

            return Ok(CreateLoginResponse(loginResponse, user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification 2FA");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Renvoie un nouveau code de vérification 2FA
    /// </summary>
    /// <param name="twoFactorToken">Token 2FA original</param>
    /// <returns>Confirmation d'envoi</returns>
    [HttpPost("resend-2fa")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendTwoFactorCode([FromBody] string twoFactorToken)
    {
        try
        {
            // Décoder le token pour récupérer l'userId
            string userId;
            try
            {
                var tokenData = Encoding.UTF8.GetString(Convert.FromBase64String(twoFactorToken));
                var parts = tokenData.Split(':');
                if (parts.Length != 2)
                    return BadRequest(new { message = "Token 2FA invalide" });
                
                userId = parts[0];
            }
            catch
            {
                return BadRequest(new { message = "Token 2FA invalide" });
            }

            // Vérifier le rate limiting
            var rateLimitKey = $"{GetClientIpAddress()}:{userId}";
            if (await _securityService.IsRateLimitExceededAsync(rateLimitKey, "two_factor_resend"))
            {
                return StatusCode(429, new { message = "Trop de demandes de renvoi. Veuillez réessayer plus tard." });
            }

            await _securityService.RecordAttemptAsync(rateLimitKey, "two_factor_resend");

            // Générer et envoyer un nouveau code
            var newCode = await _securityService.GenerateTwoFactorCodeAsync(userId);
            
            // Récupérer l'utilisateur et envoyer l'email
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new { message = "Utilisateur introuvable" });
            }
            
            await _emailService.SendTwoFactorCodeAsync(user.Email!, user.UserName!, newCode);

            // Audit du renvoi de code 2FA
            await _auditService.LogSecurityEventAsync(
                AuditActions.TwoFactorEnabled,
                userId,
                user.Email!,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                twoFactorToken,
                $"Code 2FA renvoyé depuis {GetClientIpAddress()}",
                "Information");

            _logger.LogInformation("Nouveau code 2FA envoyé pour utilisateur {UserId}", userId);

            return Ok(new { message = "Nouveau code de vérification envoyé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renvoi du code 2FA");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    private string GetClientIpAddress()
    {
        return Request.Headers.ContainsKey("X-Forwarded-For") 
            ? Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            : Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string? GetSessionToken()
    {
        // 1. Priorité au header (pour compatibilité API/mobile)
        if (Request.Headers.ContainsKey(SecurityConstants.SessionTokenHeader))
        {
            return Request.Headers[SecurityConstants.SessionTokenHeader].FirstOrDefault();
        }

        // 2. Vérifier le cookie selon la configuration
        if (ShouldUseCookies())
        {
            var cookieName = _securitySettings.CookieSettings.CookieName;
            if (Request.Cookies.ContainsKey(cookieName))
            {
                return Request.Cookies[cookieName];
            }
        }

        // 3. Fallback sur les claims (pour compatibilité)
        return User.FindFirst(SecurityConstants.SessionIdClaim)?.Value;
    }

    /// <summary>
    /// Crée une réponse de connexion appropriée selon la configuration de sécurité
    /// </summary>
    private object CreateLoginResponse(LoginDto loginDto, ApplicationUser? user = null)
    {
        // Gérer les cookies selon la configuration
        if (ShouldUseCookies() && !string.IsNullOrEmpty(loginDto.SessionToken))
        {
            SetSessionCookie(loginDto.SessionToken, loginDto.ExpiresAt);
        }

        if (_securitySettings.UseSecureResponses)
        {
            var secureResponse = _mapper.Map<SecureLoginDto>(loginDto);
            
            // Ajouter les informations utilisateur sécurisées si autorisé
            if (_securitySettings.ExposeDetailedUserInfo && user != null)
            {
                secureResponse.User = _mapper.Map<SecureUserDto>(user);
            }
            else if (user != null)
            {
                // Version ultra-minimaliste
                secureResponse.User = new SecureUserDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName
                };
            }

            // En mode cookie, ne pas exposer le token dans la réponse
            if (ShouldUseCookies() && _securitySettings.AuthenticationMethod == AuthenticationMethods.Cookie)
            {
                secureResponse.SessionToken = string.Empty;
            }
            
            return secureResponse;
        }
        
        // Mode développement : gérer les cookies mais retourner toutes les informations
        if (ShouldUseCookies() && _securitySettings.AuthenticationMethod == AuthenticationMethods.Cookie)
        {
            loginDto.SessionToken = string.Empty; // Ne pas exposer le token en mode cookie pur
        }
        
        return loginDto;
    }

    /// <summary>
    /// Détermine si les cookies doivent être utilisés selon la configuration
    /// </summary>
    private bool ShouldUseCookies()
    {
        return _securitySettings.AuthenticationMethod == AuthenticationMethods.Cookie ||
               _securitySettings.AuthenticationMethod == AuthenticationMethods.Both;
    }

    /// <summary>
    /// Configure et ajoute le cookie de session
    /// </summary>
    private void SetSessionCookie(string sessionToken, DateTime expiresAt)
    {
        var cookieSettings = _securitySettings.CookieSettings;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = cookieSettings.HttpOnly,
            Secure = cookieSettings.Secure,
            Path = cookieSettings.Path,
            Expires = expiresAt,
            IsEssential = true // Nécessaire pour RGPD
        };

        // Configuration SameSite
        cookieOptions.SameSite = cookieSettings.SameSite.ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "lax" => SameSiteMode.Lax,
            "strict" => SameSiteMode.Strict,
            _ => SameSiteMode.Strict
        };

        // Configuration du domaine si spécifié
        if (!string.IsNullOrEmpty(cookieSettings.Domain))
        {
            cookieOptions.Domain = cookieSettings.Domain;
        }

        Response.Cookies.Append(cookieSettings.CookieName, sessionToken, cookieOptions);
        
        _logger.LogInformation("Cookie de session configuré : {CookieName}, HttpOnly: {HttpOnly}, Secure: {Secure}, SameSite: {SameSite}", 
            cookieSettings.CookieName, cookieOptions.HttpOnly, cookieOptions.Secure, cookieOptions.SameSite);
    }

    /// <summary>
    /// Supprime le cookie de session
    /// </summary>
    private void ClearSessionCookie()
    {
        var cookieSettings = _securitySettings.CookieSettings;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = cookieSettings.HttpOnly,
            Secure = cookieSettings.Secure,
            Path = cookieSettings.Path,
            Expires = DateTime.UtcNow.AddDays(-1), // Date passée pour supprimer
            IsEssential = true
        };

        // Configuration SameSite
        cookieOptions.SameSite = cookieSettings.SameSite.ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "lax" => SameSiteMode.Lax,
            "strict" => SameSiteMode.Strict,
            _ => SameSiteMode.Strict
        };

        // Configuration du domaine si spécifié
        if (!string.IsNullOrEmpty(cookieSettings.Domain))
        {
            cookieOptions.Domain = cookieSettings.Domain;
        }

        Response.Cookies.Append(cookieSettings.CookieName, string.Empty, cookieOptions);
        
        _logger.LogInformation("Cookie de session supprimé : {CookieName}", cookieSettings.CookieName);
    }
}