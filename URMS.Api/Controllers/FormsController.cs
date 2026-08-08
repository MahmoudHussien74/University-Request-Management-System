using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URMS.Api.Extensions;
using URMS.Application.Contracts.Forms;
using URMS.Application.Contracts.Persistence;
using URMS.Application.DTOs.Forms;
using URMS.Domain.Entities;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/forms")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly IFormDefinitionService _formService;
    private readonly IUnitOfWork _unitOfWork;

    public FormsController(IFormDefinitionService formService, IUnitOfWork unitOfWork)
    {
        _formService = formService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Landing page: Get a lightweight summary of currently active forms (no Fields payload).
    /// </summary>
    [HttpGet("summaries")]
    [AllowAnonymous]
    public async Task<ActionResult<List<FormSummaryDto>>> GetLandingPageForms()
    {
        var now = DateTime.UtcNow;

        var summaries = await _unitOfWork.Repository<FormDefinition>()
            .GetQueryable()
            .AsNoTracking()
            .Where(f => !f.IsDeleted &&
                        f.IsActive &&
                        f.StartDate <= now &&
                        f.EndDate >= now)
            .Select(f => new FormSummaryDto(
                f.Id,
                f.TitleAr,
                f.TitleEn,
                f.Description,
                f.StartDate,
                f.EndDate,
                f.Requests.Count))
            .ToListAsync();

        return Ok(summaries);
    }

    /// <summary>
    /// Student: Get list of currently open and active forms available for submission.
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveForms()
    {
        var result = await _formService.GetActiveStudentFormsAsync();
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Student/UI: Get field schema for a specific dynamic form to render inputs (Google Forms Renderer).
    /// </summary>
    [HttpGet("{id:int}/schema")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFormSchema(int id)
    {
        var result = await _formService.GetFormByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
