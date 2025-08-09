using System.ComponentModel;

namespace BankingSessionAPI.Models.Responses;

/// <summary>
/// Réponse d'erreur standardisée de l'API
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Message d'erreur descriptif
    /// </summary>
    /// <example>Identifiants invalides</example>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Code d'erreur spécifique (optionnel)
    /// </summary>
    /// <example>AUTH_INVALID_CREDENTIALS</example>
    public string? ErrorCode { get; set; }
    
    /// <summary>
    /// Détails supplémentaires sur l'erreur (optionnel)
    /// </summary>
    /// <example>L'email ou le mot de passe fourni est incorrect</example>
    public string? Details { get; set; }
    
    /// <summary>
    /// Horodatage de l'erreur
    /// </summary>
    /// <example>2025-08-05T08:30:01.486Z</example>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Indique si l'erreur est temporaire et peut être réessayée
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool IsRetryable { get; set; }
}