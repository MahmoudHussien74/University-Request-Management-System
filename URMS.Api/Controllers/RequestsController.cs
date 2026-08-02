using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
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
    /// Student submits a new university request (Full / Extra Hours).
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Requests.Create)]
    public async Task<ActionResult<UniversityRequestResponseDto>> CreateRequest([FromBody] CreateUniversityRequestDto dto)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var result = await _requestService.CreateRequestAsync(studentId, dto);
        return CreatedAtAction(nameof(GetRequestById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Student retrieves their own submitted requests.
    /// </summary>
    [HttpGet("my")]
    [HasPermission(Permissions.Requests.ViewOwn)]
    public async Task<ActionResult<List<UniversityRequestResponseDto>>> GetMyRequests()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var result = await _requestService.GetMyRequestsAsync(studentId);
        return Ok(result);
    }

    /// <summary>
    /// Advisor / Secretary lists all requests (optional status filter).
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Requests.View)]
    public async Task<ActionResult<List<UniversityRequestResponseDto>>> GetAllRequests([FromQuery] RequestStatus? status)
    {
        var result = await _requestService.GetAllRequestsAsync(status);
        return Ok(result);
    }

    /// <summary>
    /// Get details of a specific request by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UniversityRequestResponseDto>> GetRequestById(int id)
    {
        var result = await _requestService.GetRequestByIdAsync(id);
        if (result is null)
            return NotFound($"Request with ID {id} not found.");

        return Ok(result);
    }

    /// <summary>
    /// Academic Advisor approves or rejects a student request.
    /// </summary>
    [HttpPost("{id:int}/advisor-review")]
    [HasPermission(Permissions.Requests.ApproveAdvisor)]
    public async Task<ActionResult<UniversityRequestResponseDto>> AdvisorReview(int id, [FromBody] AdvisorReviewRequestDto dto)
    {
        var advisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(advisorId))
            return Unauthorized();

        var result = await _requestService.ReviewByAdvisorAsync(id, advisorId, dto);
        return Ok(result);
    }

    /// <summary>
    /// College Secretary / Staff confirms request & finalizes completion/payment.
    /// </summary>
    [HttpPost("{id:int}/staff-confirm")]
    [HasPermission(Permissions.Requests.ConfirmStaff)]
    public async Task<ActionResult<UniversityRequestResponseDto>> StaffConfirm(int id, [FromBody] StaffConfirmRequestDto dto)
    {
        var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(staffId))
            return Unauthorized();

        var result = await _requestService.ConfirmByStaffAsync(id, staffId, dto);
        return Ok(result);
    }

    /// <summary>
    /// SuperAdmin directly overrides request status to any state (Completed, Rejected, etc.).
    /// </summary>
    [HttpPost("{id:int}/admin-override")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult<UniversityRequestResponseDto>> AdminOverride(int id, [FromBody] AdminOverrideRequestDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var result = await _requestService.OverrideStatusByAdminAsync(id, adminId, dto);
        return Ok(result);
    }
}
