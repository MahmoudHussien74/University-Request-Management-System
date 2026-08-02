using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class AcademicAdvisorConfiguration : IEntityTypeConfiguration<AcademicAdvisor>
{
    public void Configure(EntityTypeBuilder<AcademicAdvisor> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdvisorCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.AdvisorCode)
            .IsUnique();

        builder.Property(a => a.AvailabilityDays)
            .HasMaxLength(200);

        builder.Property(a => a.PendingAvailabilityDays)
            .HasMaxLength(200);

        builder.HasOne(a => a.User)
            .WithOne(u => u.Advisor)
            .HasForeignKey<AcademicAdvisor>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
