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
    /// Get all request statuses with IDs, enum names, and translated descriptions.
    /// </summary>
    [HttpGet("statuses")]
    [AllowAnonymous]
    public IActionResult GetRequestStatuses()
    {
        var statuses = _requestService.GetRequestStatuses();
        return Ok(statuses);
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
    /// Academic Advisor sends the request to student affairs by email.
    /// </summary>
    [HttpPost("{id:int}/send-to-administration")]
    [HasPermission(Permissions.Requests.ApproveAdvisor)]
    public async Task<IActionResult> SendToAdministration(int id, [FromBody] SendRequestToAdministrationDto dto)
    {
        var advisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(advisorId))
            return Unauthorized();

        var result = await _requestService.SendRequestToAdministrationAsync(id, dto, advisorId);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// <summary>
    /// Public endpoint for external administration to view the request by token.
    /// </summary>
    [HttpGet("external/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetExternalRequest(string token)
    {
        var result = await _requestService.GetRequestByTokenAsync(token);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Public endpoint for external administration to approve or reject via token.
    /// </summary>
    [HttpPost("external/{token}/respond")]
    [AllowAnonymous]
    public async Task<IActionResult> RespondExternalRequest(string token, [FromBody] ExternalAdministrationResponseDto dto)
    {
        var result = await _requestService.RespondExternalRequestAsync(token, dto);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// College Secretary confirms request and finalizes completion.
    /// </summary>
    [HttpPost("{id:int}/secretary-confirm")]
    [Authorize(Roles = AppRoles.CollegeSecretary)]
    [HasPermission(Permissions.Requests.ConfirmAdministration)]
    public async Task<IActionResult> SecretaryConfirm(int id, [FromBody] AdministrationConfirmRequestDto dto)
    {
        var secretaryId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(secretaryId))
            return Unauthorized();

        var result = await _requestService.ConfirmByAdministrationAsync(id, secretaryId, dto);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Student withdraws their own request while it is still pending.
    /// </summary>
    [HttpPost("{id:int}/withdraw")]
    [HasPermission(Permissions.Requests.ViewOwn)]
    public async Task<IActionResult> WithdrawRequest(int id)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var result = await _requestService.WithdrawRequestAsync(id, studentId);

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
