using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using URMS.Api.Extensions;
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
    public async Task<IActionResult> GetPendingStudents()
    {
        var result = await _userManagementService.GetPendingStudentsAsync();

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Approve a student account — allows the student to login.
    /// </summary>
    [HttpPost("{studentId}/approve")]
    [HasPermission(Permissions.Users.ApproveRegistration)]
    public async Task<IActionResult> ApproveStudent(string studentId)
    {
        var result = await _userManagementService.ApproveStudentAsync(studentId);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Deactivate a user account — blocks login.
    /// </summary>
    [HttpPost("{userId}/deactivate")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> DeactivateAccount(string userId)
    {
        var result = await _userManagementService.DeactivateAccountAsync(userId);

        return result.IsSuccess
            ? Ok(new { message = "Account deactivated successfully." })
            : result.ToProblem();
    }

    /// <summary>
    /// Reactivate a previously deactivated account.
    /// </summary>
    [HttpPost("{userId}/reactivate")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> ReactivateAccount(string userId)
    {
        var result = await _userManagementService.ReactivateAccountAsync(userId);

        return result.IsSuccess
            ? Ok(new { message = "Account reactivated successfully." })
            : result.ToProblem();
    }

    /// <summary>
    /// Get all students with their activation status.
    /// Admin/Secretary see all students; Advisor sees only assigned students.
    /// </summary>
    [HttpGet("students-activation")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> GetStudentsForActivation()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        var result = await _userManagementService.GetAllStudentsForActivationAsync(userId, roles);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Update student profile data (admin operation).
    /// </summary>
    [HttpPut("{userId}")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateStudent(string userId, [FromBody] UpdateStudentRequest request)
    {
        var result = await _userManagementService.UpdateStudentAsync(userId, request);

        return result.IsSuccess
            ? Ok(new { message = "Student data updated successfully." })
            : result.ToProblem();
    }
}
