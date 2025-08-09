using System.ComponentModel;

namespace BankingSessionAPI.Models.DTOs;

/// <summary>
/// Informations détaillées de l'utilisateur
/// </summary>
public class UserDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur
    /// </summary>
    /// <example>a1b2c3d4-e5f6-7890-abcd-ef1234567890</example>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Nom d'utilisateur
    /// </summary>
    /// <example>testuser</example>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Adresse email de l'utilisateur
    /// </summary>
    /// <example>testuser@banking-api.com</example>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Prénom de l'utilisateur
    /// </summary>
    /// <example>Jean</example>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nom de famille de l'utilisateur
    /// </summary>
    /// <example>Dupont</example>
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nom complet de l'utilisateur
    /// </summary>
    /// <example>Jean Dupont</example>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Numéro de téléphone (optionnel)
    /// </summary>
    /// <example>+33 6 12 34 56 78</example>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Adresse physique (optionnel)
    /// </summary>
    /// <example>123 Rue de la Paix, 75001 Paris, France</example>
    public string? Address { get; set; }
    
    /// <summary>
    /// Notes administratives (optionnel)
    /// </summary>
    /// <example>Utilisateur VIP</example>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Indique si le compte utilisateur est actif
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Date de création du compte
    /// </summary>
    /// <example>2025-01-15T10:30:00.000Z</example>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Date de dernière connexion
    /// </summary>
    /// <example>2025-08-05T07:45:23.123Z</example>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Rôles assignés à l'utilisateur
    /// </summary>
    /// <example>["User", "Customer"]</example>
    public IList<string> Roles { get; set; } = new List<string>();
    
    /// <summary>
    /// Nombre de sessions actives
    /// </summary>
    /// <example>2</example>
    public int ActiveSessionsCount { get; set; }
    
    /// <summary>
    /// Indique si l'utilisateur doit changer son mot de passe
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool RequiresPasswordChange { get; set; }
    
    /// <summary>
    /// Indique si l'email a été confirmé
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool EmailConfirmed { get; set; }
    
    /// <summary>
    /// Indique si le numéro de téléphone a été confirmé
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool PhoneNumberConfirmed { get; set; }
    
    /// <summary>
    /// Indique si l'authentification à deux facteurs est activée
    /// </summary>
    /// <example>true</example>
    [DefaultValue(false)]
    public bool TwoFactorEnabled { get; set; }
    
    /// <summary>
    /// Date de fin de verrouillage du compte (si verrouillé)
    /// </summary>
    /// <example>null</example>
    public DateTimeOffset? LockoutEnd { get; set; }
    
    /// <summary>
    /// Indique si le verrouillage de compte est activé
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool LockoutEnabled { get; set; }
    
    /// <summary>
    /// Nombre d'échecs de connexion consécutifs
    /// </summary>
    /// <example>0</example>
    public int AccessFailedCount { get; set; }
}