using System.ComponentModel.DataAnnotations;

namespace BankingSessionAPI.Models.Entities;

public class UserSession
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DeviceId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DeviceName { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? RevokedBy { get; set; }

    [MaxLength(200)]
    public string? RevocationReason { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsValid => IsActive && !IsRevoked && !IsExpired;
}