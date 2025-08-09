using System.ComponentModel;

namespace BankingSessionAPI.Models.DTOs;

/// <summary>
/// Réponse de connexion utilisateur
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Token de session sécurisé pour les requêtes authentifiées
    /// </summary>
    /// <example>nWcoL_iw96A-dhMk1R5uPX6W7c9CCj-SxwMhClpplpc</example>
    public string SessionToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Date et heure d'expiration de la session (UTC)
    /// </summary>
    /// <example>2025-08-05T08:30:01.486Z</example>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Informations détaillées de l'utilisateur connecté
    /// </summary>
    public UserDto? User { get; set; }
    
    /// <summary>
    /// Informations de la session créée
    /// </summary>
    public SessionInfoDto? Session { get; set; }
    
    /// <summary>
    /// Indique si une vérification à deux facteurs est requise
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool RequiresTwoFactor { get; set; }
    
    /// <summary>
    /// Token temporaire pour la vérification 2FA (si RequiresTwoFactor = true)
    /// </summary>
    /// <example>null</example>
    public string? TwoFactorToken { get; set; }
    
    /// <summary>
    /// Message informatif pour l'utilisateur
    /// </summary>
    /// <example>Connexion réussie</example>
    public string? Message { get; set; }
    
    /// <summary>
    /// Indique si la connexion a réussi
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool Success { get; set; }
}