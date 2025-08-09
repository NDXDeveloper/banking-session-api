namespace BankingSessionAPI.Models.DTOs;

/// <summary>
/// Version sécurisée des informations utilisateur pour les réponses de connexion
/// Contient uniquement les informations essentielles et non-sensibles
/// </summary>
public class SecureUserDto
{
    /// <summary>
    /// Prénom de l'utilisateur
    /// </summary>
    /// <example>Nicolas</example>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur
    /// </summary>
    /// <example>DEOUX</example>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet de l'utilisateur
    /// </summary>
    /// <example>Nicolas DEOUX</example>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Adresse email de l'utilisateur (optionnel selon la configuration)
    /// </summary>
    /// <example>ndxdev@gmail.com</example>
    public string? Email { get; set; }
}