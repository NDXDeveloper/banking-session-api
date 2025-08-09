using System.ComponentModel.DataAnnotations;

namespace BankingSessionAPI.Models.Entities;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string Level { get; set; } = "Information";

    [MaxLength(200)]
    public string? SessionId { get; set; }

    [MaxLength(500)]
    public string? AdditionalInfo { get; set; }

    public bool IsSuccessful { get; set; } = true;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}