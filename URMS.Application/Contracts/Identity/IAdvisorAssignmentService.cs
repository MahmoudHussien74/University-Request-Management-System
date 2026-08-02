using URMS.Application.DTOs.AdvisorAssignment;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Identity;

public interface IAdvisorAssignmentService
{
    /// <summary>
    /// Bulk assign university codes to a specific advisor (SuperAdmin uploads college data).
    /// </summary>
    Task<Result<int>> BulkAssignAsync(BulkAssignStudentsDto dto);

    /// <summary>
    /// Get all assignments for a specific advisor.
    /// </summary>
    Task<Result<List<AdvisorStudentAssignmentDto>>> GetAssignmentsByAdvisorAsync(string advisorId);

    /// <summary>
    /// Get all assignments in the system.
    /// </summary>
    Task<Result<List<AdvisorStudentAssignmentDto>>> GetAllAssignmentsAsync();

    /// <summary>
    /// Remove a specific assignment by university code.
    /// </summary>
    Task<Result> RemoveAssignmentAsync(string universityCode);

    /// <summary>
    /// Reassign a student code to a different advisor.
    /// </summary>
    Task<Result> ReassignAsync(AssignStudentDto dto);
}
