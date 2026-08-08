using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Identity;

public interface IUserManagementService
{
    /// <summary>
    /// Get all students with IsApproved = false (pending advisor review).
    /// Advisors see only their assigned students; Admin/Secretary see all.
    /// </summary>
    Task<Result<List<PendingStudentDto>>> GetPendingStudentsAsync(string callerUserId, IList<string> callerRoles);

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
    /// Get all students with their activation status for admin management.
    /// Advisors see only their assigned students; Admin/Secretary see all.
    /// </summary>
    Task<Result<List<StudentActivationDto>>> GetAllStudentsForActivationAsync(string callerUserId, IList<string> callerRoles);

    /// <summary>
    /// Update student profile data (admin operation).
    /// </summary>
    Task<Result> UpdateStudentAsync(string userId, UpdateStudentRequest request);
}
