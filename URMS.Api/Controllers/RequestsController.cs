using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Api.Extensions;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Enums;
using URMS.Infrastructure.PermissionAuthorization;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IUniversityRequestService _requestService;

    public RequestsController(IUniversityRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>
    /// Get all available university request types with IDs and Arabic names for frontend dropdowns.
    /// </summary>
    [HttpGet("types")]
    [AllowAnonymous]
    public IActionResult GetRequestTypes()
    {
        var types = _requestService.GetRequestTypes();
        return Ok(types);
    }

    /// <summary>
    /// Student submits a new university request (Full / Extra Hours).
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Requests.Create)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateUniversityRequestDto dto)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var result = await _requestService.CreateRequestAsync(studentId, dto);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetRequestById), new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    /// Student retrieves their own submitted requests.
    /// </summary>
    [HttpGet("my")]
    [HasPermission(Permissions.Requests.ViewOwn)]
    public async Task<IActionResult> GetMyRequests()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var result = await _requestService.GetMyRequestsAsync(studentId);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Advisor / Secretary lists all requests (optional status filter).
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Requests.View)]
    public async Task<IActionResult> GetAllRequests([FromQuery] RequestStatus? status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdvisor = User.IsInRole(AppRoles.AcademicAdvisor);
        var isStaffOrAdmin = User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.CollegeSecretary);

        Result<List<UniversityRequestResponseDto>> result;

        if (isAdvisor && !isStaffOrAdmin && !string.IsNullOrEmpty(userId))
        {
            result = await _requestService.GetAdvisorRequestsAsync(userId, status);
        }
        else
        {
            result = await _requestService.GetAllRequestsAsync(status);
        }

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Get details of a specific request by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRequestById(int id)
    {
        var result = await _requestService.GetRequestByIdAsync(id);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Academic Advisor approves or rejects a student request.
    /// </summary>
    [HttpPost("{id:int}/advisor-review")]
    [HasPermission(Permissions.Requests.ApproveAdvisor)]
    public async Task<IActionResult> AdvisorReview(int id, [FromBody] AdvisorReviewRequestDto dto)
    {
        var advisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(advisorId))
            return Unauthorized();

        var result = await _requestService.ReviewByAdvisorAsync(id, advisorId, dto);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// College Secretary / Staff confirms request & finalizes completion/payment.
    /// </summary>
    [HttpPost("{id:int}/staff-confirm")]
    [HasPermission(Permissions.Requests.ConfirmStaff)]
    public async Task<IActionResult> StaffConfirm(int id, [FromBody] StaffConfirmRequestDto dto)
    {
        var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(staffId))
            return Unauthorized();

        var result = await _requestService.ConfirmByStaffAsync(id, staffId, dto);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// SuperAdmin directly overrides request status to any state (Completed, Rejected, etc.).
    /// </summary>
    [HttpPost("{id:int}/admin-override")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> AdminOverride(int id, [FromBody] AdminOverrideRequestDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var result = await _requestService.OverrideStatusByAdminAsync(id, adminId, dto);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
