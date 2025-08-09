using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BankingSessionAPI.Models.Requests;

/// <summary>
/// Requête de connexion utilisateur
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Adresse email de l'utilisateur
    /// </summary>
    /// <example>testuser@banking-api.com</example>
    [Required(ErrorMessage = "L'adresse email est requise")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe de l'utilisateur
    /// </summary>
    /// <example>TestUser123!</example>
    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant unique de l'appareil
    /// </summary>
    /// <example>mobile-app-12345</example>
    [Required(ErrorMessage = "L'identifiant de l'appareil est requis")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Nom convivial de l'appareil (optionnel)
    /// </summary>
    /// <example>iPhone de Jean</example>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Maintenir la session plus longtemps (30 jours au lieu de 8 heures)
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool RememberMe { get; set; }
}