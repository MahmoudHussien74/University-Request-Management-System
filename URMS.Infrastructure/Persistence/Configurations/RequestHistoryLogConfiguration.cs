using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence.Configurations;

public class RequestHistoryLogConfiguration : IEntityTypeConfiguration<RequestHistoryLog>
{
    public void Configure(EntityTypeBuilder<RequestHistoryLog> builder)
    {
        builder.Property(l => l.ActionMessage).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(1000);

        builder.HasOne(l => l.UniversityRequest)
            .WithMany(r => r.HistoryLogs)
            .HasForeignKey(l => l.UniversityRequestId)
            .OnDelete(DeleteBehavior.Cascade); // If request is deleted, delete history

        builder.HasOne(l => l.ActionBy)
            .WithMany()
            .HasForeignKey(l => l.ActionById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
