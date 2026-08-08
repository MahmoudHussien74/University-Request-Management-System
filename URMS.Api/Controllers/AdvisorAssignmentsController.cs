using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Api.Extensions;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.AdvisorAssignment;
using URMS.Domain.Constants;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class AdvisorAssignmentsController : ControllerBase
{
    private readonly IAdvisorAssignmentService _assignmentService;

    public AdvisorAssignmentsController(IAdvisorAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Bulk assign student university codes to a specific advisor.
    /// Used by SuperAdmin to upload college advisor-student mapping.
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkAssign([FromBody] BulkAssignStudentsDto dto)
    {
        var result = await _assignmentService.BulkAssignAsync(dto);

        return result.IsSuccess
            ? Ok(new { message = $"{result.Value} student code(s) assigned successfully." })
            : result.ToProblem();
    }

    /// <summary>
    /// Get all assignments for a specific advisor.
    /// </summary>
    [HttpGet("advisor/{advisorId}")]
    public async Task<IActionResult> GetByAdvisor(string advisorId)
    {
        var result = await _assignmentService.GetAssignmentsByAdvisorAsync(advisorId);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Get all assignments in the system.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _assignmentService.GetAllAssignmentsAsync();

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Remove a specific assignment by university code.
    /// </summary>
    [HttpDelete("{universityCode}")]
    public async Task<IActionResult> Remove(string universityCode)
    {
        var result = await _assignmentService.RemoveAssignmentAsync(universityCode);

        return result.IsSuccess
            ? Ok(new { message = "Assignment removed successfully." })
            : result.ToProblem();
    }

    /// <summary>
    /// Reassign a student code to a different advisor.
    /// </summary>
    [HttpPut("reassign")]
    public async Task<IActionResult> Reassign([FromBody] AssignStudentDto dto)
    {
        var result = await _assignmentService.ReassignAsync(dto);

        return result.IsSuccess
            ? Ok(new { message = "Student reassigned successfully." })
            : result.ToProblem();
    }
}
