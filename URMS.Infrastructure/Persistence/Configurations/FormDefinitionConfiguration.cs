using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public void Configure(EntityTypeBuilder<FormDefinition> builder)
    {
        builder.Property(f => f.TitleAr).HasMaxLength(200).IsRequired();
        builder.Property(f => f.TitleEn).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(1000);
        builder.Property(f => f.ClosedReasonMessage).HasMaxLength(500);

        builder.HasMany(f => f.Fields)
            .WithOne(field => field.FormDefinition)
            .HasForeignKey(field => field.FormDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
