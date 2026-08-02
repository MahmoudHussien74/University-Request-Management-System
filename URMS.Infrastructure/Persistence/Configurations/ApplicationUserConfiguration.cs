using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstNameAr)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.SecondNameAr)
            .HasMaxLength(50);

        builder.Property(u => u.ThirdNameAr)
            .HasMaxLength(50);

        builder.Property(u => u.LastNameAr)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.FirstNameEn)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.SecondNameEn)
            .HasMaxLength(50);

        builder.Property(u => u.ThirdNameEn)
            .HasMaxLength(50);

        builder.Property(u => u.LastNameEn)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.AlternatePhone)
            .HasMaxLength(20);

        builder.Property(u => u.UserType)
            .IsRequired();
    }
}
