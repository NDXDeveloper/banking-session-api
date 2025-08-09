using BankingSessionAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingSessionAPI.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AccountNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.AccountName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.AccountType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EUR");

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.Property(e => e.CreditLimit)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.InterestRate)
            .HasColumnType("decimal(5,4)")
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(e => e.AccountNumber)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_AccountNumber");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_Accounts_UserId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Accounts_IsActive");

        builder.HasOne(e => e.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}