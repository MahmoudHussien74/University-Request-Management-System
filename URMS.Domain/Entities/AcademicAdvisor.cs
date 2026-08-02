using URMS.Domain.Common;

namespace URMS.Domain.Entities;

public class AcademicAdvisor : BaseEntity
{
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    public string AdvisorCode { get; set; } = default!;
    public string? AvailabilityDays { get; set; }        // Stored as comma-separated: "Sunday,Monday,Wednesday"
    public string? PendingAvailabilityDays { get; set; } // Pending change awaiting approval
}
