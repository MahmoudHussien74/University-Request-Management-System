using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class UniversityRequestConfiguration : IEntityTypeConfiguration<UniversityRequest>
{
    public void Configure(EntityTypeBuilder<UniversityRequest> builder)
    {
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);
        builder.Property(r => r.ConfirmationToken).HasMaxLength(256);
        builder.Property(r => r.ExternalAdministrationEmail).HasMaxLength(256);
        builder.Property(r => r.ExternalAdministrationOtpCodeHash).HasMaxLength(256);
        builder.Property(r => r.ExternalAdministrationResponseNotes).HasColumnType("nvarchar(max)");
        builder.Property(r => r.AdditionalDataJson).HasColumnType("nvarchar(max)");

        builder.HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Advisor)
            .WithMany()
            .HasForeignKey(r => r.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Administration)
            .WithMany()
            .HasForeignKey(r => r.AdministrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.FormDefinition)
            .WithMany(f => f.Requests)
            .HasForeignKey(r => r.FormDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        // ─── Indexes for Query Performance (Checklist Item 6) ───
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.ConfirmationToken);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.StudentId);
        builder.HasIndex(r => r.AdvisorId);

        
    }
}