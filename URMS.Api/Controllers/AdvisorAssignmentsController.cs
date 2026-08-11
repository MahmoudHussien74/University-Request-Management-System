using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Api.Extensions;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.AdvisorAssignment;
using URMS.Domain.Constants;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvisorAssignmentsController : ControllerBase
{
    private readonly IAdvisorAssignmentService _assignmentService;

    public AdvisorAssignmentsController(IAdvisorAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Get all students assigned to the currently logged-in Academic Advisor with search & pagination.
    /// Accessible by: AcademicAdvisor
    /// </summary>
    [HttpGet("my-students")]
    [Authorize(Roles = AppRoles.AcademicAdvisor)]
    public async Task<IActionResult> GetMyStudents(
        [FromQuery] string? searchColumn = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var advisorUserId = User.GetUserId();
        var result = await _assignmentService.GetMyStudentsAsync(advisorUserId, searchColumn, searchTerm, pageNumber, pageSize);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Bulk assign student university codes to a specific advisor.
    /// Used by SuperAdmin to upload college advisor-student mapping.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> BulkAssign([FromBody] BulkAssignStudentsDto dto)
    {
        var result = await _assignmentService.BulkAssignAsync(dto);

        return result.IsSuccess
            ? Ok(new { message = $"{result.Value} student code(s) assigned successfully." })
            : result.ToProblem();
    }

    /// <summary>
    /// Get all assignments for a specific advisor with search & pagination.
    /// </summary>
    [HttpGet("advisor/{advisorId}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.CollegeSecretary}")]
    public async Task<IActionResult> GetByAdvisor(
        string advisorId,
        [FromQuery] string? searchColumn = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await _assignmentService.GetAssignmentsByAdvisorAsync(advisorId, searchColumn, searchTerm, pageNumber, pageSize);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Get all assignments in the system grouped by advisor with search & pagination.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.CollegeSecretary}")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchColumn = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await _assignmentService.GetAllAssignmentsAsync(searchColumn, searchTerm, pageNumber, pageSize);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Remove a specific assignment by university code.
    /// </summary>
    [HttpDelete("{universityCode}")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
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
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> Reassign([FromBody] AssignStudentDto dto)
    {
        var result = await _assignmentService.ReassignAsync(dto);

        return result.IsSuccess
            ? Ok(new { message = "Student reassigned successfully." })
            : result.ToProblem();
    }

    /// <summary>
    /// Import advisor-student assignments directly from an Excel file (.xlsx / .xls).
    /// </summary>
    [HttpPost("import-excel")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "الرجاء اختيار ملف Excel لرفعه." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
            return BadRequest(new { message = "صيغة الملف غير مدعومة. يجب رفع ملف Excel بصيغة .xlsx أو .xls" });

        using var stream = file.OpenReadStream();
        var result = await _assignmentService.ImportFromExcelAsync(stream);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
