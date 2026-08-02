using Microsoft.AspNetCore.Mvc;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Constants;
using URMS.Infrastructure.PermissionAuthorization;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Get all students pending approval (IsApproved = false).
    /// Accessible by: AcademicAdvisor, CollegeSecretary, SuperAdmin
    /// </summary>
    [HttpGet("pending-students")]
    [HasPermission(Permissions.Users.ApproveRegistration)]
    public async Task<ActionResult<List<PendingStudentDto>>> GetPendingStudents()
    {
        var result = await _userManagementService.GetPendingStudentsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Approve a student account — allows the student to login.
    /// </summary>
    [HttpPost("{studentId}/approve")]
    [HasPermission(Permissions.Users.ApproveRegistration)]
    public async Task<ActionResult<UserResponse>> ApproveStudent(string studentId)
    {
        var result = await _userManagementService.ApproveStudentAsync(studentId);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate a user account — blocks login.
    /// </summary>
    [HttpPost("{userId}/deactivate")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> DeactivateAccount(string userId)
    {
        await _userManagementService.DeactivateAccountAsync(userId);
        return Ok(new { message = "Account deactivated successfully." });
    }

    /// <summary>
    /// Reactivate a previously deactivated account.
    /// </summary>
    [HttpPost("{userId}/reactivate")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> ReactivateAccount(string userId)
    {
        await _userManagementService.ReactivateAccountAsync(userId);
        return Ok(new { message = "Account reactivated successfully." });
    }
}
