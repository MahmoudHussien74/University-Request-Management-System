namespace URMS.Application.DTOs.Requests;

public record UniversityRequestResponseDto(
    int Id,
    string StudentId,
    string StudentNameAr,
    string StudentNameEn,
    string? UniversityCode,
    int? FormDefinitionId,
    string? FormTitleAr,
    string? FormTitleEn,
    string Status,
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
