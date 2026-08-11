using URMS.Application.Common.Pagination;
using URMS.Domain.Abstractions;

namespace URMS.Application.DTOs.AdvisorAssignment;

/// <summary>
/// DTO for bulk assigning students to an advisor (SuperAdmin uploads from college data).
/// </summary>
public record BulkAssignStudentsDto(
    string AdvisorId,
    List<string> UniversityCodes
);

/// <summary>
/// DTO for single assignment.
/// </summary>
public record AssignStudentDto(
    string UniversityCode,
    string AdvisorId
);

/// <summary>
/// DTO representing a student assigned to an advisor.
/// </summary>
public record AssignedStudentDto(
    int AssignmentId,
    string UniversityCode,
    string? StudentNameAr,
    string? StudentNameEn,
    bool IsStudentRegistered,
    DateTime AssignedAt
);

/// <summary>
/// Grouped Response DTO containing Advisor Info and their assigned students list.
/// </summary>
public record AdvisorAssignmentsGroupDto(
    string AdvisorId,
    string AdvisorNameAr,
    string AdvisorNameEn,
    string AdvisorCode,
    string Email,
    int TotalStudents,
    PaginatedList<AssignedStudentDto> Students
);

/// <summary>
/// Legacy Response DTO showing assignment details.
/// </summary>
public record AdvisorStudentAssignmentDto(
    int Id,
    string UniversityCode,
    string AdvisorId,
    string AdvisorName,
    DateTime AssignedAt,
    bool IsStudentRegistered
);

/// <summary>
/// Response DTO for importing advisor-student assignments from Excel file.
/// </summary>
public record ImportExcelAssignmentsResponseDto(
    int TotalRowsProcessed,
    int SuccessfulAssignments,
    int SkippedAssignments,
    List<string> Errors
);

/// <summary>
/// Detailed student item for logged-in advisor view.
/// </summary>
public record AdvisorMyStudentItemDto(
    int AssignmentId,
    string UniversityCode,
    bool IsRegistered,
    DateTime AssignedAt,
    string? StudentId,
    string? FullNameAr,
    string? FullNameEn,
    string? NationalId,
    string? Email,
    string? PhoneNumber,
    string? AlternatePhone,
    string? Address
);

/// <summary>
/// Response for logged-in advisor's assigned students list with pagination.
/// </summary>
public record AdvisorMyStudentsResponseDto(
    string AdvisorId,
    string AdvisorNameAr,
    string AdvisorNameEn,
    string AdvisorCode,
    int TotalStudents,
    int RegisteredStudentsCount,
    int UnregisteredStudentsCount,
    PaginatedList<AdvisorMyStudentItemDto> Students
);
