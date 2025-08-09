using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingSessionAPI.Models.Entities;

public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0;

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CreditLimit { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal InterestRate { get; set; } = 0;

    public DateTime? LastTransactionDate { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}