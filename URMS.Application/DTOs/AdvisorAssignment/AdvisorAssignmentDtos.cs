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
/// Response DTO showing assignment details.
/// </summary>
public record AdvisorStudentAssignmentDto(
    int Id,
    string UniversityCode,
    string AdvisorId,
    string AdvisorName,
    DateTime AssignedAt,
    bool IsStudentRegistered
);
