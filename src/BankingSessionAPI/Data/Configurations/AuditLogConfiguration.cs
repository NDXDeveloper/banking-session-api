using BankingSessionAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingSessionAPI.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EntityId)
            .HasMaxLength(100);

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.UserName)
            .HasMaxLength(256);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.Level)
            .HasMaxLength(50)
            .HasDefaultValue("Information");

        builder.Property(e => e.SessionId)
            .HasMaxLength(200);

        builder.Property(e => e.AdditionalInfo)
            .HasMaxLength(500);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.Timestamp)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(e => new { e.UserId, e.Timestamp })
            .HasDatabaseName("IX_AuditLogs_UserId_Timestamp");

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(e => e.EntityType)
            .HasDatabaseName("IX_AuditLogs_EntityType");

        builder.HasOne(e => e.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}