using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstNameAr).HasMaxLength(50).IsRequired();
        builder.Property(u => u.SecondNameAr).HasMaxLength(50);
        builder.Property(u => u.ThirdNameAr).HasMaxLength(50);
        builder.Property(u => u.LastNameAr).HasMaxLength(50).IsRequired();

        builder.Property(u => u.FirstNameEn).HasMaxLength(50).IsRequired();
        builder.Property(u => u.SecondNameEn).HasMaxLength(50);
        builder.Property(u => u.ThirdNameEn).HasMaxLength(50);
        builder.Property(u => u.LastNameEn).HasMaxLength(50).IsRequired();

        builder.Property(u => u.UniversityCode).HasMaxLength(20);
        builder.HasIndex(u => u.UniversityCode).IsUnique().HasFilter("[UniversityCode] IS NOT NULL");

        builder.Property(u => u.NationalId).HasMaxLength(14);
        builder.HasIndex(u => u.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL");

        builder.Property(u => u.AdvisorCode).HasMaxLength(20);

        builder.Property(u => u.GPA).HasPrecision(3, 2);
    }
}
