using URMS.Application.DTOs.Forms;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Forms;

public interface IFormDefinitionService
{
    Task<Result<FormDefinitionResponseDto>> CreateFormAsync(CreateFormDefinitionDto dto, string createdBy);
    Task<Result<FormDefinitionResponseDto>> UpdateFormAsync(int id, UpdateFormDefinitionDto dto, string updatedBy);
    Task<Result<FormDefinitionResponseDto>> ToggleFormStatusAsync(int id, ToggleFormStatusDto dto);
    Task<Result<bool>> DeleteFormAsync(int id);
    Task<Result<FormFieldResponseDto>> AddFieldToFormAsync(int formId, CreateFormFieldDto dto);
    Task<Result<bool>> DeleteFormFieldAsync(int formId, int fieldId);
    Task<Result<FormDefinitionResponseDto>> GetFormByIdAsync(int id);
    Task<Result<List<FormDefinitionResponseDto>>> GetAllAdminFormsAsync();
    Task<Result<List<FormDefinitionResponseDto>>> GetActiveStudentFormsAsync();
    Task<Result<List<FormSummaryDto>>> GetLandingPageFormsAsync();
    Task<Result<bool>> ValidateSubmissionAnswersAsync(int formDefinitionId, Dictionary<string, string>? answers);
}
