using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class FormFieldDefinitionConfiguration : IEntityTypeConfiguration<FormFieldDefinition>
{
    public void Configure(EntityTypeBuilder<FormFieldDefinition> builder)
    {
        builder.Property(field => field.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(field => field.LabelAr).HasMaxLength(200).IsRequired();
        builder.Property(field => field.LabelEn).HasMaxLength(200).IsRequired();
        builder.Property(field => field.Placeholder).HasMaxLength(200);
        builder.Property(field => field.OptionsJson).HasColumnType("nvarchar(max)");
    }
}
