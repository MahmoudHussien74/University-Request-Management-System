using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Identity;

public interface IUserManagementService
{
    /// <summary>
    /// Get all students with IsApproved = false (pending advisor review).
    /// </summary>
    Task<Result<List<PendingStudentDto>>> GetPendingStudentsAsync();

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
    /// </summary>
    Task<Result<List<StudentActivationDto>>> GetAllStudentsForActivationAsync();
}
