using URMS.Domain.Common;
using URMS.Domain.Enums;

namespace URMS.Domain.Entities;

/// <summary>
/// Core entity representing a student request in the university system.
/// Supports academic registration, certificates, transcripts, and custom requests.
/// </summary>
public class UniversityRequest : AuditableEntity
{
    public RequestType Type { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    // ─── Dynamic Form Definition Link ───
    public int? FormDefinitionId { get; set; }
    public FormDefinition? FormDefinition { get; set; }

    // ─── Student Info (Optional depending on request type) ───
    public decimal? GPA { get; set; }
    public int? RequestedHours { get; set; }

    // ─── Notes, Rejection & Dynamic Form Data ───
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdditionalDataJson { get; set; }     // Holds dynamic key-value form metadata as JSON

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

    // ─── Payment (for Extra Hours or paid services) ───
    public bool? IsPaymentCompleted { get; set; }
    public DateTime? PaymentCompletedAt { get; set; }

    // ─── Student (Owner) ───
    public string StudentId { get; set; } = default!;
    public ApplicationUser Student { get; set; } = default!;

    // ─── Completion ───
    public DateTime? CompletedAt { get; set; }

    // ─── History Logs ───
    public ICollection<RequestHistoryLog> HistoryLogs { get; set; } = new List<RequestHistoryLog>();
}
