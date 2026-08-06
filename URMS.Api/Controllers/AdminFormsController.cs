using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Api.Extensions;
using URMS.Application.Contracts.Forms;
using URMS.Application.DTOs.Forms;
using URMS.Domain.Constants;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/admin/forms")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class AdminFormsController : ControllerBase
{
    private readonly IFormDefinitionService _formService;

    public AdminFormsController(IFormDefinitionService formService)
    {
        _formService = formService;
    }

    /// <summary>
    /// Super Admin: Get list of all forms (active, inactive, deleted count, request counters).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllForms()
    {
        var result = await _formService.GetAllAdminFormsAsync();
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Super Admin: Get form details and schema by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFormById(int id)
    {
        var result = await _formService.GetFormByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Super Admin: Create a new dynamic request form (Google Forms Style).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateForm([FromBody] CreateFormDefinitionDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SuperAdmin";
        var result = await _formService.CreateFormAsync(dto, adminId);
        return result.IsSuccess ? CreatedAtAction(nameof(GetFormById), new { id = result.Value.Id }, result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Super Admin: Update an existing form title, description, or fields.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateForm(int id, [FromBody] UpdateFormDefinitionDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SuperAdmin";
        var result = await _formService.UpdateFormAsync(id, dto, adminId);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Super Admin: Toggle open/close status of a form for students.
    /// </summary>
    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleFormStatus(int id, [FromBody] ToggleFormStatusDto dto)
    {
        var result = await _formService.ToggleFormStatusAsync(id, dto);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Super Admin: Soft-delete a form (prevents new submissions while preserving previous audit history).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteForm(int id)
    {
        var result = await _formService.DeleteFormAsync(id);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    /// Super Admin: Add a new field to an existing form.
    /// </summary>
    [HttpPost("{formId:int}/fields")]
    public async Task<IActionResult> AddFieldToForm(int formId, [FromBody] CreateFormFieldDto dto)
    {
        var result = await _formService.AddFieldToFormAsync(formId, dto);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Super Admin: Delete a specific field from a form by field ID.
    /// </summary>
    [HttpDelete("{formId:int}/fields/{fieldId:int}")]
    public async Task<IActionResult> DeleteFormField(int formId, int fieldId)
    {
        var result = await _formService.DeleteFormFieldAsync(formId, fieldId);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
