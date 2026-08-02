using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UniversityCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.UniversityCode)
            .IsUnique();

        builder.Property(s => s.NationalId)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.NationalId)
            .IsUnique();

        builder.Property(s => s.Address)
            .HasMaxLength(250);

        builder.Property(s => s.GPA)
            .HasPrecision(3, 2);

        builder.HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.AcademicAdvisor)
            .WithMany()
            .HasForeignKey(s => s.AcademicAdvisorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
