using System.ComponentModel;

namespace BankingSessionAPI.Models.Responses;

/// <summary>
/// Réponse de succès standardisée de l'API
/// </summary>
public class SuccessResponse
{
    /// <summary>
    /// Message de confirmation
    /// </summary>
    /// <example>Déconnexion réussie</example>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Indique si l'opération a réussi
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool Success { get; set; }
    
    /// <summary>
    /// Horodatage de la réponse
    /// </summary>
    /// <example>2025-08-05T08:30:01.486Z</example>
    public DateTime Timestamp { get; set; }
}