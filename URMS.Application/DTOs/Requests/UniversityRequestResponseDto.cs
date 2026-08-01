using URMS.Domain.Enums;

namespace URMS.Application.DTOs.Requests;

public record UniversityRequestResponseDto(
    int Id,
    string StudentId,
    string StudentNameAr,
    string StudentNameEn,
    string? UniversityCode,
    RequestType RequestType,
    RequestStatus Status,
    decimal GPA,
    int RequestedHours,
    string? Notes,
    string? AdvisorId,
    string? AdvisorName,
    string? RejectionReason,
    string? StaffId,
    string? StaffName,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? CompletedAt
);
