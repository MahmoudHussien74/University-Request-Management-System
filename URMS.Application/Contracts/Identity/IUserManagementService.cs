using URMS.Application.Common.Pagination;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Identity;

public interface IUserManagementService
{
    /// <summary>
    /// Get all students with IsApproved = false (pending advisor review), supporting optional search & pagination.
    /// Advisors see only their assigned students; Admin/Secretary see all.
    /// </summary>
    Task<Result<PaginatedList<PendingStudentDto>>> GetPendingStudentsAsync(string callerUserId, IList<string> callerRoles, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);

    /// <summary>
    /// Advisor approves a student account — enables login.
    /// </summary>
    Task<Result<UserResponse>> ApproveStudentAsync(string studentId);

    /// <summary>
    /// Deactivate a student account — blocks login.
    /// </summary>
    Task<Result> DeactivateAccountAsync(string userId);

    /// <summary>
    /// Reactivate a previously deactivated account.
    /// </summary>
    Task<Result> ReactivateAccountAsync(string userId);

    /// <summary>
    /// Get all students with their activation status for admin management, supporting optional search & pagination.
    /// Advisors see only their assigned students; Admin/Secretary see all.
    /// </summary>
    Task<Result<PaginatedList<StudentActivationDto>>> GetAllStudentsForActivationAsync(string callerUserId, IList<string> callerRoles, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);

    /// <summary>
    /// Update student profile data (admin operation).
    /// </summary>
    Task<Result> UpdateStudentAsync(string userId, UpdateStudentRequest request);

    /// <summary>
    /// Create a single Academic Advisor account.
    /// </summary>
    Task<Result<AdvisorDto>> CreateAdvisorAsync(CreateAdvisorDto dto);

    /// <summary>
    /// Bulk create Academic Advisor accounts.
    /// </summary>
    Task<Result<BulkCreateAdvisorsResponseDto>> BulkCreateAdvisorsAsync(BulkCreateAdvisorsDto dto);

    /// <summary>
    /// Get all Academic Advisors in the system, supporting optional search & pagination.
    /// </summary>
    Task<Result<PaginatedList<AdvisorDto>>> GetAllAdvisorsAsync(string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);

    /// <summary>
    /// Delete a user account by userId (SuperAdmin operation).
    /// </summary>
    Task<Result> DeleteUserAsync(string userId, string callerUserId);
}
