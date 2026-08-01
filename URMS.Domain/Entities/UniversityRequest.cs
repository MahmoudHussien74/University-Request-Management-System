using URMS.Domain.Common;
using URMS.Domain.Enums;

namespace URMS.Domain.Entities;

/// <summary>
/// Core entity representing a student request in the university system.
/// Supports: Full Hours Registration (GPA 1.95–2.0) and Extra Hours Registration (GPA 3.3–3.75).
/// </summary>
public class UniversityRequest : AuditableEntity
{
    public RequestType Type { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    // ─── Student Info (snapshot at time of request) ───
    public decimal GPA { get; set; }
    public int? RequestedHours { get; set; }           // Only for ExtraHoursRegistration

    // ─── Notes & Rejection ───
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }

    // ─── Advisor Review ───
    public string? AdvisorId { get; set; }
    public ApplicationUser? Advisor { get; set; }
    public DateTime? AdvisorReviewedAt { get; set; }
    public bool? IsGpaConfirmedByAdvisor { get; set; }

    // ─── Staff Confirmation ───
    public string? StaffId { get; set; }
    public ApplicationUser? Staff { get; set; }
    public DateTime? StaffConfirmedAt { get; set; }
    public string? ConfirmationToken { get; set; }     // Token sent via email link

    // ─── Payment (for Extra Hours only) ───
    public bool? IsPaymentCompleted { get; set; }
    public DateTime? PaymentCompletedAt { get; set; }

    // ─── Student (Owner) ───
    public string StudentId { get; set; } = default!;
    public ApplicationUser Student { get; set; } = default!;

    // ─── Completion ───
    public DateTime? CompletedAt { get; set; }
}
