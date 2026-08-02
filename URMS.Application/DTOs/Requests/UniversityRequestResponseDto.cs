namespace URMS.Application.DTOs.Requests;

public record UniversityRequestResponseDto(
    int Id,
    string StudentId,
    string StudentNameAr,
    string StudentNameEn,
    string? UniversityCode,
    string RequestType,
    string Status,
    decimal? GPA,
    int? RequestedHours,
    string? Notes,
    Dictionary<string, string>? AdditionalData,
    string? AdvisorId,
    string? AdvisorName,
    string? RejectionReason,
    string? StaffId,
    string? StaffName,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? CompletedAt,
    List<RequestHistoryLogDto>? HistoryLogs = null
);
