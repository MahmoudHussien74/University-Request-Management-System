using URMS.Application.Common.Pagination;
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
    /// Get all assignments for a specific advisor grouped with advisor info, supporting optional search & pagination.
    /// </summary>
    Task<Result<AdvisorAssignmentsGroupDto>> GetAssignmentsByAdvisorAsync(string advisorId, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);

    /// <summary>
    /// Get all assignments in the system grouped by advisor, supporting optional search & pagination.
    /// </summary>
    Task<Result<PaginatedList<AdvisorAssignmentsGroupDto>>> GetAllAssignmentsAsync(string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);

    /// <summary>
    /// Remove a specific assignment by university code.
    /// </summary>
    Task<Result> RemoveAssignmentAsync(string universityCode);

    /// <summary>
    /// Reassign a student code to a different advisor.
    /// </summary>
    Task<Result> ReassignAsync(AssignStudentDto dto);

    /// <summary>
    /// Import advisor-student assignments directly from an Excel file (.xlsx / .xls).
    /// Matches advisor by Arabic full name and links the university code.
    /// </summary>
    Task<Result<ImportExcelAssignmentsResponseDto>> ImportFromExcelAsync(Stream fileStream);

    /// <summary>
    /// Get all students assigned to the logged-in advisor with optional search & pagination.
    /// </summary>
    Task<Result<AdvisorMyStudentsResponseDto>> GetMyStudentsAsync(string advisorUserId, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
}
