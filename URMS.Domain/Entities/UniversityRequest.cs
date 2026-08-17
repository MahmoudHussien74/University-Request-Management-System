using URMS.Domain.Abstractions;
using URMS.Domain.Common;
using URMS.Domain.Constants;
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

    // ═══════════════════════════════════════════════════
    // ─── Domain Methods (State Transitions & Business Rules) ───
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Processes an advisor's review decision (approve or reject).
    /// </summary>
    public Result ReviewByAdvisor(string advisorId, bool isApproved, string? rejectionReason)
    {
        if (Status != RequestStatus.Pending && Status != RequestStatus.AdvisorApproved)
            return Result.Failure(RequestErrors.InvalidStatusForAdvisorReview);

        AdvisorId = advisorId;
        AdvisorReviewedAt = DateTime.UtcNow;

        var oldStatus = Status;

        if (isApproved)
        {
            Status = RequestStatus.AdvisorApproved;
        }
        else
        {
            Status = RequestStatus.Rejected;
            RejectionReason = rejectionReason;
        }

        AddHistoryLog(advisorId, oldStatus, Status,
            isApproved ? RequestLogMessages.ApprovedByAdvisor : RequestLogMessages.RejectedByAdvisor,
            rejectionReason);

        return Result.Success();
    }

    /// <summary>
    /// Transitions the request to SentToAdministration state and sets up OTP/token data.
    /// OTP generation and email sending remain in the Application Service (infrastructure concerns).
    /// </summary>
    public Result SendToAdministration(string advisorId, string administrationEmail,
        string otpCodeHash, string confirmationToken, DateTime otpExpiresAt, string? historyNotes)
    {
        if (Status == RequestStatus.Completed || Status == RequestStatus.Rejected)
            return Result.Failure(RequestErrors.InvalidStatusForSendEmail);

        if (Status != RequestStatus.AdvisorApproved)
            return Result.Failure(RequestErrors.InvalidStatusForSendEmail);

        var oldStatus = Status;

        AdvisorId = advisorId;
        AdvisorReviewedAt = DateTime.UtcNow;
        Status = RequestStatus.SentToAdministration;
        ExternalAdministrationEmail = administrationEmail;
        ExternalAdministrationSentAt = DateTime.UtcNow;
        ExternalAdministrationOtpSentAt = DateTime.UtcNow;
        ExternalAdministrationOtpExpiresAt = otpExpiresAt;
        ConfirmationToken = confirmationToken;
        ExternalAdministrationOtpCodeHash = otpCodeHash;
        ExternalAdministrationResponseNotes = null;
        ExternalAdministrationRespondedAt = null;

        AddHistoryLog(advisorId, oldStatus, Status,
            RequestLogMessages.SentToAdministration, historyNotes);

        return Result.Success();
    }

    /// <summary>
    /// Processes an external administration response (approve/reject via OTP-verified link).
    /// OTP verification itself should be done in the Application Service before calling this.
    /// </summary>
    public Result RespondByExternalAdministration(bool isApproved, string? notes)
    {
        if (ExternalAdministrationRespondedAt.HasValue)
            return Result.Failure(RequestErrors.InvalidStatusForAdministrationConfirm);

        if (Status != RequestStatus.SentToAdministration)
            return Result.Failure(RequestErrors.InvalidStatusForAdministrationConfirm);

        var oldStatus = Status;

        ExternalAdministrationRespondedAt = DateTime.UtcNow;
        ExternalAdministrationResponseNotes = notes;

        // Clear OTP data after successful response
        ConfirmationToken = null;
        ExternalAdministrationOtpCodeHash = null;
        ExternalAdministrationOtpSentAt = null;
        ExternalAdministrationOtpExpiresAt = null;

        if (isApproved)
        {
            Status = RequestStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
        else
        {
            Status = RequestStatus.Rejected;
            RejectionReason = notes;
        }

        AddHistoryLog(StudentId, oldStatus, Status,
            RequestLogMessages.ExternalAdministrationResponded, notes);

        return Result.Success();
    }

    /// <summary>
    /// Processes an internal administration confirmation (approve/reject).
    /// </summary>
    public Result ConfirmByAdministration(string administrationId, bool isApproved, string? confirmationNotes)
    {
        if (Status != RequestStatus.SentToAdministration)
            return Result.Failure(RequestErrors.InvalidStatusForAdministrationConfirm);

        AdministrationId = administrationId;
        AdministrationConfirmedAt = DateTime.UtcNow;

        var oldStatus = Status;

        if (isApproved)
        {
            Status = RequestStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
        else
        {
            Status = RequestStatus.Rejected;
            RejectionReason = confirmationNotes;
        }

        AddHistoryLog(administrationId, oldStatus, Status,
            isApproved ? RequestLogMessages.ConfirmedByAdministration : RequestLogMessages.RejectedByAdministration,
            confirmationNotes);

        return Result.Success();
    }

    /// <summary>
    /// Allows a SuperAdmin to override the request status directly.
    /// </summary>
    public Result OverrideStatusByAdmin(string adminId, RequestStatus targetStatus, string? reasonOrNotes)
    {
        if (targetStatus == RequestStatus.SentToAdministration)
            return Result.Failure(RequestErrors.InvalidStatusForAdminOverride);

        var oldStatus = Status;
        Status = targetStatus;

        if (targetStatus == RequestStatus.Completed)
        {
            CompletedAt = DateTime.UtcNow;
            AdministrationId = adminId;
            AdministrationConfirmedAt = DateTime.UtcNow;

            if (AdvisorReviewedAt == null)
            {
                AdvisorId = adminId;
                AdvisorReviewedAt = DateTime.UtcNow;
            }
        }
        else if (targetStatus == RequestStatus.Rejected)
        {
            RejectionReason = reasonOrNotes;
        }
        else if (targetStatus == RequestStatus.AdvisorApproved)
        {
            AdvisorId = adminId;
            AdvisorReviewedAt = DateTime.UtcNow;
        }

        AddHistoryLog(adminId, oldStatus, Status,
            RequestLogMessages.AdminOverride, reasonOrNotes);

        return Result.Success();
    }

    // ─── Private Helpers ───

    private void AddHistoryLog(string actionById, RequestStatus oldStatus, RequestStatus newStatus,
        string actionMessage, string? notes)
    {
        HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = actionById,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ActionMessage = actionMessage,
            Notes = notes
        });
    }
}
