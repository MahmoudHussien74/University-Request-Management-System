using URMS.Domain.Common;

namespace URMS.Domain.Entities;

/// <summary>
/// Tracks requests by Academic Advisors to change their availability days.
/// Changes require approval from the Guidance Committee Chair.
/// </summary>
public class AvailabilityChangeRequest : BaseEntity
{
    public string CurrentDays { get; set; } = default!;
    public string RequestedDays { get; set; } = default!;
    public bool? IsApproved { get; set; }              // null = pending, true = approved, false = rejected
    public string? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // ─── Navigation ───
    public string AdvisorId { get; set; } = default!;
    public ApplicationUser Advisor { get; set; } = default!;
}
