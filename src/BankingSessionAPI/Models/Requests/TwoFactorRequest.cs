using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BankingSessionAPI.Models.Requests;

/// <summary>
/// Requête de vérification à deux facteurs
/// </summary>
public class TwoFactorRequest
{
    /// <summary>
    /// Token temporaire de vérification 2FA reçu lors de la connexion
    /// </summary>
    /// <example>YTFiMmMzZDQtZTVmNi03ODkwOjE3MjU1MTgyMDE0ODY=</example>
    [Required(ErrorMessage = "Le token 2FA est requis")]
    public string TwoFactorToken { get; set; } = string.Empty;

    /// <summary>
    /// Code de vérification à 6 chiffres reçu par email
    /// </summary>
    /// <example>123456</example>
    [Required(ErrorMessage = "Le code de vérification est requis")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Le code doit contenir exactement 6 chiffres")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Le code doit contenir uniquement des chiffres")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant unique de l'appareil (optionnel)
    /// </summary>
    /// <example>mobile-app-12345</example>
    public string? DeviceId { get; set; }
    
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