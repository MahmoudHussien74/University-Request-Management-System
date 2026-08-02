using URMS.Application.DTOs.Auth;

namespace URMS.Application.Contracts.Identity;

public interface IUserManagementService
{
    /// <summary>
    /// Get all students with IsApproved = false (pending advisor review).
    /// </summary>
    Task<List<PendingStudentDto>> GetPendingStudentsAsync();

    /// <summary>
    /// Advisor approves a student account — enables login.
    /// </summary>
    Task<UserResponse> ApproveStudentAsync(string studentId);

    /// <summary>
    /// Deactivate a student account — blocks login.
    /// </summary>
    Task DeactivateAccountAsync(string userId);

    /// <summary>
    /// Reactivate a previously deactivated account.
    /// </summary>
    Task ReactivateAccountAsync(string userId);
}
