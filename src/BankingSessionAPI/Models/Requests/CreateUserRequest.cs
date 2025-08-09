using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BankingSessionAPI.Models.Requests;

/// <summary>
/// Requête de création d'utilisateur (Admin uniquement)
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Adresse email de l'utilisateur (unique)
    /// </summary>
    /// <example>nouveau.user@banking-api.com</example>
    [Required(ErrorMessage = "L'adresse email est requise")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [MaxLength(256, ErrorMessage = "L'email ne peut pas dépasser 256 caractères")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nom d'utilisateur (unique)
    /// </summary>
    /// <example>nouveau.user</example>
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères")]
    [MaxLength(50, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 50 caractères")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Prénom de l'utilisateur
    /// </summary>
    /// <example>Jean</example>
    [Required(ErrorMessage = "Le prénom est requis")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur
    /// </summary>
    /// <example>Dupont</example>
    [Required(ErrorMessage = "Le nom de famille est requis")]
    [MaxLength(100, ErrorMessage = "Le nom de famille ne peut pas dépasser 100 caractères")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Numéro de téléphone (optionnel)
    /// </summary>
    /// <example>+33123456789</example>
    [Phone(ErrorMessage = "Format de téléphone invalide")]
    [MaxLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Adresse physique (optionnel)
    /// </summary>
    /// <example>123 Rue de la Paix, 75001 Paris, France</example>
    [MaxLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
    public string? Address { get; set; }

    /// <summary>
    /// Mot de passe temporaire de l'utilisateur
    /// </summary>
    /// <example>TempPassword123!</example>
    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    [MaxLength(100, ErrorMessage = "Le mot de passe ne peut pas dépasser 100 caractères")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indique si l'utilisateur est actif dès la création
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indique si l'email est confirmé dès la création
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool EmailConfirmed { get; set; } = true;

    /// <summary>
    /// Indique si l'authentification à deux facteurs doit être activée
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Rôles à assigner à l'utilisateur (optionnel)
    /// </summary>
    /// <example>["User"]</example>
    public List<string>? Roles { get; set; }

    /// <summary>
    /// Notes administratives sur l'utilisateur (optionnel)
    /// </summary>
    /// <example>Utilisateur créé pour les tests</example>
    [MaxLength(500, ErrorMessage = "Les notes ne peuvent pas dépasser 500 caractères")]
    public string? Notes { get; set; }
}