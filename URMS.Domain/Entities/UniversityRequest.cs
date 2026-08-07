using URMS.Domain.Common;
using URMS.Domain.Enums;

namespace URMS.Domain.Entities;

/// <summary>
/// Core entity representing a student request in the university system.
/// Powered dynamically by FormDefinition schema and AdditionalDataJson.
/// </summary>
public class UniversityRequest : AuditableEntity
{
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    // ─── Dynamic Form Definition Link ───
    public int? FormDefinitionId { get; set; }
    public FormDefinition? FormDefinition { get; set; }

    // ─── Dynamic Form Data & Rejection Reason ───
    public string? RejectionReason { get; set; }
    public string? AdditionalDataJson { get; set; }     // Holds dynamic key-value form metadata as JSON

    // ─── Advisor Review ───
    public string? AdvisorId { get; set; }
    public ApplicationUser? Advisor { get; set; }
    public DateTime? AdvisorReviewedAt { get; set; }

    // ─── Administration / Staff Confirmation ───
    public string? AdministrationId { get; set; }
    public ApplicationUser? Administration { get; set; }
    public DateTime? AdministrationConfirmedAt { get; set; }
    public string? ConfirmationToken { get; set; }     // Token sent via email link
    public string? ExternalAdministrationEmail { get; set; }
    public DateTime? ExternalAdministrationSentAt { get; set; }
    public string? ExternalAdministrationOtpCodeHash { get; set; }
    public DateTime? ExternalAdministrationOtpSentAt { get; set; }
    public DateTime? ExternalAdministrationOtpExpiresAt { get; set; }
    public DateTime? ExternalAdministrationRespondedAt { get; set; }
    public string? ExternalAdministrationResponseNotes { get; set; }

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
