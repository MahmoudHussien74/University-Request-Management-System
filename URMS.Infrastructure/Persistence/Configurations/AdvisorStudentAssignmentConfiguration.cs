using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class AdvisorStudentAssignmentConfiguration : IEntityTypeConfiguration<AdvisorStudentAssignment>
{
    public void Configure(EntityTypeBuilder<AdvisorStudentAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UniversityCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.UniversityCode)
            .IsUnique();

        builder.HasOne(a => a.Advisor)
            .WithMany()
            .HasForeignKey(a => a.AdvisorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
