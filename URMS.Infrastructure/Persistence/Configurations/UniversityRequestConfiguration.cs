using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;
namespace URMS.Infrastructure.Persistence.Configurations;
public class UniversityRequestConfiguration : IEntityTypeConfiguration<UniversityRequest>
{
    public void Configure(EntityTypeBuilder<UniversityRequest> builder)
    {
        builder.Property(r => r.GPA).HasPrecision(3, 2).IsRequired(false);
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);
        builder.Property(r => r.ConfirmationToken).HasMaxLength(256);
        builder.Property(r => r.AdditionalDataJson).HasColumnType("nvarchar(max)");

        builder.HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Advisor)
            .WithMany()
            .HasForeignKey(r => r.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Staff)
            .WithMany()
            .HasForeignKey(r => r.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.FormDefinition)
            .WithMany(f => f.Requests)
            .HasForeignKey(r => r.FormDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}