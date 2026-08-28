using Microsoft.Extensions.Logging;
using URMS.Application.DTOs.Forms;

namespace URMS.Application.Services;

public class FormDefinitionService : IFormDefinitionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormDefinitionRepository _formRepo;
    private readonly ILogger<FormDefinitionService> _logger;

    public FormDefinitionService(IUnitOfWork unitOfWork, IFormDefinitionRepository formRepo, ILogger<FormDefinitionService> logger)
    {
        _unitOfWork = unitOfWork;
        _formRepo = formRepo;
        _logger = logger;
    }

    public async Task<Result<FormDefinitionResponseDto>> CreateFormAsync(CreateFormDefinitionDto dto, string createdBy)
    {
        var form = new FormDefinition
        {
            TitleAr = dto.TitleAr,
            TitleEn = dto.TitleEn,
            Description = dto.Description,
            IsActive = dto.IsActive,
            IsDeleted = false,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Fields != null && dto.Fields.Count > 0)
        {
            foreach (var f in dto.Fields)
            {
                form.Fields.Add(new FormFieldDefinition
                {
                    FieldKey = GenerateFieldKey(f.LabelEn),
                    LabelAr = f.LabelAr,
                    LabelEn = f.LabelEn,
                    Placeholder = f.Placeholder,
                    Type = f.Type,
                    IsRequired = f.IsRequired,
                    Order = f.Order,
                    OptionsJson = f.Options != null && f.Options.Count > 0 ? JsonSerializer.Serialize(f.Options) : null
                });
            }
        }

        await _formRepo.AddAsync(form);
        await _unitOfWork.CompleteAsync();

        return Result.Success(MapToResponseDto(form, 0));
    }

    public async Task<Result<FormDefinitionResponseDto>> UpdateFormAsync(int id, UpdateFormDefinitionDto dto, string updatedBy)
    {
        var form = await _formRepo.GetByIdWithDetailsAsync(id);

        if (form is null)
            return Result.Failure<FormDefinitionResponseDto>(FormErrors.FormNotFound);

        form.TitleAr = dto.TitleAr;
        form.TitleEn = dto.TitleEn;
        form.Description = dto.Description;
        form.IsActive = dto.IsActive;
        form.StartDate = dto.StartDate;
        form.EndDate = dto.EndDate;
        form.UpdatedBy = updatedBy;
        form.UpdatedAt = DateTime.UtcNow;

        // Clear existing fields and replace with updated fields
        form.Fields.Clear();

        if (dto.Fields != null && dto.Fields.Count > 0)
        {
            foreach (var f in dto.Fields)
            {
                form.Fields.Add(new FormFieldDefinition
                {
                    FormDefinitionId = form.Id,
                    FieldKey = GenerateFieldKey(f.LabelEn),
                    LabelAr = f.LabelAr,
                    LabelEn = f.LabelEn,
                    Placeholder = f.Placeholder,
                    Type = f.Type,
                    IsRequired = f.IsRequired,
                    Order = f.Order,
                    OptionsJson = f.Options != null && f.Options.Count > 0 ? JsonSerializer.Serialize(f.Options) : null
                });
            }
        }

        await _unitOfWork.CompleteAsync();

        var requestsCount = await _formRepo.GetRequestCountAsync(form.Id);
        return Result.Success(MapToResponseDto(form, requestsCount));
    }

    public async Task<Result<FormDefinitionResponseDto>> ToggleFormStatusAsync(int id, ToggleFormStatusDto dto)
    {
        var form = await _formRepo.GetByIdWithDetailsAsync(id);

        if (form is null)
            return Result.Failure<FormDefinitionResponseDto>(FormErrors.FormNotFound);

        form.IsActive = dto.IsActive;
        if (!dto.IsActive)
        {
            form.ClosedReasonMessage = dto.ClosedReasonMessage;
        }
        else
        {
            form.ClosedReasonMessage = null;
        }

        form.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CompleteAsync();

        var requestsCount = await _formRepo.GetRequestCountAsync(form.Id);
        return Result.Success(MapToResponseDto(form, requestsCount));
    }

    public async Task<Result<bool>> DeleteFormAsync(int id)
    {
        var form = await _formRepo.GetByIdAsync(id);

        if (form is null || form.IsDeleted)
            return Result.Failure<bool>(FormErrors.FormNotFound);

        form.IsDeleted = true;
        form.IsActive = false;
        form.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CompleteAsync();

        return Result.Success(true);
    }

    public async Task<Result<FormFieldResponseDto>> AddFieldToFormAsync(int formId, CreateFormFieldDto dto)
    {
        var form = await _formRepo.GetByIdWithFieldsAsync(formId);

        if (form is null)
            return Result.Failure<FormFieldResponseDto>(FormErrors.FormNotFound);

        var nextOrder = dto.Order > 0 ? dto.Order : (form.Fields.Count + 1);

        var newField = new FormFieldDefinition
        {
            FormDefinitionId = form.Id,
            FieldKey = GenerateFieldKey(dto.LabelEn),
            LabelAr = dto.LabelAr,
            LabelEn = dto.LabelEn,
            Placeholder = dto.Placeholder,
            Type = dto.Type,
            IsRequired = dto.IsRequired,
            Order = nextOrder,
            OptionsJson = dto.Options != null && dto.Options.Count > 0 ? JsonSerializer.Serialize(dto.Options) : null
        };

        var fieldRepo = _unitOfWork.Repository<FormFieldDefinition>();
        await fieldRepo.AddAsync(newField);
        await _unitOfWork.CompleteAsync();

        List<string>? options = null;
        if (!string.IsNullOrEmpty(newField.OptionsJson))
        {
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(newField.OptionsJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize OptionsJson for field '{FieldKey}'", newField.FieldKey);
            }
        }

        var responseDto = new FormFieldResponseDto(
            newField.Id,
            newField.FieldKey,
            newField.LabelAr,
            newField.LabelEn,
            newField.Placeholder,
            newField.Type.ToString(),
            newField.IsRequired,
            newField.Order,
            options
        );

        return Result.Success(responseDto);
    }

    public async Task<Result<bool>> DeleteFormFieldAsync(int formId, int fieldId)
    {
        var form = await _formRepo.GetByIdWithFieldsAsync(formId);

        if (form is null)
            return Result.Failure<bool>(FormErrors.FormNotFound);

        var field = form.Fields.FirstOrDefault(f => f.Id == fieldId);
        if (field is null)
            return Result.Failure<bool>(FormErrors.FieldNotFound);

        var fieldRepo = _unitOfWork.Repository<FormFieldDefinition>();
        fieldRepo.Delete(field);
        await _unitOfWork.CompleteAsync();

        return Result.Success(true);
    }

    public async Task<Result<FormDefinitionResponseDto>> GetFormByIdAsync(int id)
    {
        var form = await _formRepo.GetByIdWithDetailsAsync(id);

        if (form is null)
            return Result.Failure<FormDefinitionResponseDto>(FormErrors.FormNotFound);

        var requestsCount = await _formRepo.GetRequestCountAsync(form.Id);
        return Result.Success(MapToResponseDto(form, requestsCount));
    }

    public async Task<Result<List<FormDefinitionResponseDto>>> GetAllAdminFormsAsync()
    {
        var forms = await _formRepo.GetAllForAdminAsync();
        var formIds = forms.Select(f => f.Id).ToList();
        var requestCounts = await _formRepo.GetRequestCountsByFormIdsAsync(formIds);

        return Result.Success(forms.Select(f => MapToResponseDto(f, requestCounts.GetValueOrDefault(f.Id, 0))).ToList());
    }

    public async Task<Result<List<FormDefinitionResponseDto>>> GetActiveStudentFormsAsync()
    {
        var now = DateTime.UtcNow;
        var forms = await _formRepo.GetActiveForStudentsAsync(now);
        var formIds = forms.Select(f => f.Id).ToList();
        var requestCounts = await _formRepo.GetRequestCountsByFormIdsAsync(formIds);

        return Result.Success(forms.Select(f => MapToResponseDto(f, requestCounts.GetValueOrDefault(f.Id, 0))).ToList());
    }

    public async Task<Result<List<FormSummaryDto>>> GetLandingPageFormsAsync()
    {
        var now = DateTime.UtcNow;
        var summaries = await _formRepo.GetActiveSummariesAsync(now);
        return Result.Success(summaries);
    }

    public async Task<Result<bool>> ValidateSubmissionAnswersAsync(int formDefinitionId, Dictionary<string, string>? answers)
    {
        var now = DateTime.UtcNow;

        var form = await _formRepo.GetByIdWithFieldsAsync(formDefinitionId);

        if (form is null)
            return Result.Failure<bool>(FormErrors.FormNotFound);

        if (!form.IsActive || (form.StartDate.HasValue && form.StartDate.Value > now) || (form.EndDate.HasValue && form.EndDate.Value < now))
            return Result.Failure<bool>(FormErrors.FormClosed);

        // Convert dictionary to case-insensitive comparison
        var answersDict = answers != null
            ? new Dictionary<string, string>(answers, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in form.Fields)
        {
            var hasValue = (answersDict.TryGetValue(field.FieldKey, out var val1) && !string.IsNullOrWhiteSpace(val1))
                        || (answersDict.TryGetValue(field.Id.ToString(), out var val2) && !string.IsNullOrWhiteSpace(val2));

            if (field.IsRequired && !hasValue)
            {
                return Result.Failure<bool>(FormErrors.RequiredFieldMissing(field.LabelAr));
            }
        }

        return Result.Success(true);
    }

    private FormDefinitionResponseDto MapToResponseDto(FormDefinition form, int requestsCount)
    {
        var fieldDtos = form.Fields?.OrderBy(f => f.Order).Select(f =>
        {
            List<string>? options = null;
            if (!string.IsNullOrEmpty(f.OptionsJson))
            {
                try
                {
                    options = JsonSerializer.Deserialize<List<string>>(f.OptionsJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize OptionsJson for field '{FieldKey}'", f.FieldKey);
                }
            }

            return new FormFieldResponseDto(
                f.Id,
                f.FieldKey,
                f.LabelAr,
                f.LabelEn,
                f.Placeholder,
                f.Type.ToString(),
                f.IsRequired,
                f.Order,
                options
            );
        }).ToList() ?? new List<FormFieldResponseDto>();

        return new FormDefinitionResponseDto(
            form.Id,
            form.TitleAr,
            form.TitleEn,
            form.Description,
            form.IsActive,
            form.IsDeleted,
            form.ClosedReasonMessage,
            form.StartDate,
            form.EndDate,
            form.CreatedAt,
            fieldDtos,
            requestsCount
        );
    }

    private static string GenerateFieldKey(string labelEn)
    {
        if (string.IsNullOrWhiteSpace(labelEn))
            return "field_" + Guid.NewGuid().ToString("N")[..8];

        var words = System.Text.RegularExpressions.Regex.Matches(labelEn, @"[A-Za-z0-9]+")
            .Select(m => m.Value)
            .ToList();

        if (words.Count == 0)
            return "field_" + Guid.NewGuid().ToString("N")[..8];

        var camelCase = words[0].ToLowerInvariant() + string.Concat(words.Skip(1).Select(w => char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w[1..].ToLowerInvariant() : "")));
        return camelCase;
    }
}
