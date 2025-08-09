namespace BankingSessionAPI.Models.DTOs;

/// <summary>
/// Version sécurisée de la réponse de connexion
/// Contient uniquement les informations essentielles pour l'authentification
/// </summary>
public class SecureLoginDto
{
    /// <summary>
    /// Token de session pour l'authentification
    /// </summary>
    /// <example>abc123def456ghi789</example>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Date et heure d'expiration de la session
    /// </summary>
    /// <example>2025-08-05T15:30:01.486Z</example>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Informations utilisateur sécurisées (optionnel)
    /// </summary>
    public SecureUserDto? User { get; set; }

    /// <summary>
    /// Indique si l'authentification à deux facteurs est requise
    /// </summary>
    /// <example>false</example>
    public bool RequiresTwoFactor { get; set; } = false;

    /// <summary>
    /// Token pour l'étape d'authentification à deux facteurs
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIs...</example>
    public string? TwoFactorToken { get; set; }

    /// <summary>
    /// Message de réponse
    /// </summary>
    /// <example>Connexion réussie</example>
    public string? Message { get; set; }

    /// <summary>
    /// Indique si l'opération a réussi
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; } = false;
}