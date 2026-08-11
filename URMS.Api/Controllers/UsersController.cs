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
    /// Get all students pending approval (IsApproved = false) with search and pagination.
    /// Accessible by: AcademicAdvisor, CollegeSecretary, SuperAdmin
    /// </summary>
    [HttpGet("pending-students")]
    [HasPermission(Permissions.Users.ApproveRegistration)]
    public async Task<IActionResult> GetPendingStudents(
        [FromQuery] string? searchColumn = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await _userManagementService.GetPendingStudentsAsync(User.GetUserId(), User.GetUserRoles(), searchColumn, searchTerm, pageNumber, pageSize);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Approve a student account — allows the student to login.
    /// </summary>
    [HttpPost("{studentId}/approve")]
    [HasPermission(Permissions.Users.ApproveRegistration)]
    public async Task<IActionResult> ApproveStudent(string studentId)
    {
        var result = await _userManagementService.ApproveStudentAsync(studentId);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Deactivate a user account — blocks login.
    /// </summary>
    [HttpPost("{userId}/deactivate")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> DeactivateAccount(string userId)
    {
        var result = await _userManagementService.DeactivateAccountAsync(userId);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Reactivate a previously deactivated account.
    /// </summary>
    [HttpPost("{userId}/reactivate")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> ReactivateAccount(string userId)
    {
        var result = await _userManagementService.ReactivateAccountAsync(userId);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Get all students with their activation status with search and pagination.
    /// Admin/Secretary see all students; Advisor sees only assigned students.
    /// </summary>
    [HttpGet("students-activation")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> GetStudentsForActivation(
        [FromQuery] string? searchColumn = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await _userManagementService.GetAllStudentsForActivationAsync(User.GetUserId(), User.GetUserRoles(), searchColumn, searchTerm, pageNumber, pageSize);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Update student profile data (admin operation).
    /// </summary>
    [HttpPut("{userId}")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateStudent(string userId, [FromBody] UpdateStudentRequest request)
    {
        var result = await _userManagementService.UpdateStudentAsync(userId, request);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Create a single Academic Advisor account.
    /// Accessible by: SuperAdmin
    /// </summary>
    [HttpPost("advisors")]
    [HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> CreateAdvisor([FromBody] CreateAdvisorDto dto)
    {
        var result = await _userManagementService.CreateAdvisorAsync(dto);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Bulk create Academic Advisor accounts with unified/default password.
    /// Accessible by: SuperAdmin
    /// </summary>
    [HttpPost("advisors/bulk")]
    [HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> BulkCreateAdvisors([FromBody] BulkCreateAdvisorsDto dto)
    {
        var result = await _userManagementService.BulkCreateAdvisorsAsync(dto);
        return result.ToResponse(HttpContext);
    }

    /// <summary>
    /// Get list of all Academic Advisors in the system with search and pagination.
    /// Accessible by: SuperAdmin, CollegeSecretary
    /// </summary>
    [HttpGet("advisors")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetAllAdvisors(
        [FromQuery] string? searchColumn = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await _userManagementService.GetAllAdvisorsAsync(searchColumn, searchTerm, pageNumber, pageSize);
        return result.ToResponse(HttpContext);
    }
}
