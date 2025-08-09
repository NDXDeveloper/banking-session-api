using System.ComponentModel;

namespace BankingSessionAPI.Models.DTOs;

/// <summary>
/// Informations détaillées d'une session utilisateur
/// </summary>
public class SessionInfoDto
{
    /// <summary>
    /// Identifiant unique de la session
    /// </summary>
    /// <example>sess_a1b2c3d4e5f6789012345</example>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Identifiant unique de l'appareil
    /// </summary>
    /// <example>mobile-app-12345</example>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Nom convivial de l'appareil
    /// </summary>
    /// <example>iPhone de Jean</example>
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// Chaîne User-Agent du navigateur/application
    /// </summary>
    /// <example>Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15</example>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Adresse IP de connexion
    /// </summary>
    /// <example>192.168.1.10</example>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Localisation géographique approximative
    /// </summary>
    /// <example>Paris, France</example>
    public string? Location { get; set; }
    
    /// <summary>
    /// Date et heure de création de la session
    /// </summary>
    /// <example>2025-08-05T07:30:01.486Z</example>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Date et heure d'expiration de la session
    /// </summary>
    /// <example>2025-08-05T15:30:01.486Z</example>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Date et heure du dernier accès
    /// </summary>
    /// <example>2025-08-05T08:15:30.123Z</example>
    public DateTime? LastAccessedAt { get; set; }
    
    /// <summary>
    /// Indique si la session est active
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Indique si c'est la session actuelle
    /// </summary>
    /// <example>true</example>
    [DefaultValue(false)]
    public bool IsCurrentSession { get; set; }
    
    /// <summary>
    /// Nombre de minutes restantes avant expiration
    /// </summary>
    /// <example>420</example>
    public int RemainingMinutes { get; set; }
    
    /// <summary>
    /// Indique si la session a expiré
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool IsExpired { get; set; }
    
    /// <summary>
    /// Nom d'utilisateur de la session
    /// </summary>
    /// <example>testuser</example>
    public string? UserName { get; set; }
    
    /// <summary>
    /// Email de l'utilisateur de la session
    /// </summary>
    /// <example>testuser@banking-api.com</example>
    public string? UserEmail { get; set; }
}