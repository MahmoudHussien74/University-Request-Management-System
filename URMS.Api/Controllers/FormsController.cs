using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Api.Extensions;
using URMS.Application.Contracts.Forms;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/forms")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly IFormDefinitionService _formService;

    public FormsController(IFormDefinitionService formService)
    {
        _formService = formService;
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
