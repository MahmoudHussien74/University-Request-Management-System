using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Persistence;
using URMS.Application.Contracts.Forms;
using URMS.Application.DTOs.Forms;
using URMS.Domain.Abstractions;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Infrastructure.Services;

public class FormDefinitionService : IFormDefinitionService
{
    private readonly IUnitOfWork _unitOfWork;

    public FormDefinitionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FormDefinitionResponseDto>> CreateFormAsync(CreateFormDefinitionDto dto, string createdBy)
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();

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
                    FieldKey = f.FieldKey,
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

        await formRepo.AddAsync(form);
        await _unitOfWork.CompleteAsync();

        return Result.Success(MapToResponseDto(form, 0));
    }

    public async Task<Result<FormDefinitionResponseDto>> UpdateFormAsync(int id, UpdateFormDefinitionDto dto, string updatedBy)
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();
        var fieldRepo = _unitOfWork.Repository<FormFieldDefinition>();

        var form = await formRepo.FindOneAsync(
            f => f.Id == id && !f.IsDeleted,
            q => q.Include(f => f.Fields).Include(f => f.Requests)
        );

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
                    FieldKey = f.FieldKey,
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

        return Result.Success(MapToResponseDto(form, form.Requests?.Count ?? 0));
    }

    public async Task<Result<FormDefinitionResponseDto>> ToggleFormStatusAsync(int id, ToggleFormStatusDto dto)
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();

        var form = await formRepo.FindOneAsync(
            f => f.Id == id && !f.IsDeleted,
            q => q.Include(f => f.Fields).Include(f => f.Requests)
        );

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

        return Result.Success(MapToResponseDto(form, form.Requests?.Count ?? 0));
    }

    public async Task<Result<bool>> DeleteFormAsync(int id)
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();

        var form = await formRepo.GetByIdAsync(id);

        if (form is null || form.IsDeleted)
            return Result.Failure<bool>(FormErrors.FormNotFound);

        form.IsDeleted = true;
        form.IsActive = false;
        form.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CompleteAsync();

        return Result.Success(true);
    }

    public async Task<Result<FormDefinitionResponseDto>> GetFormByIdAsync(int id)
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();

        var form = await formRepo.FindOneAsync(
            f => f.Id == id && !f.IsDeleted,
            q => q.Include(f => f.Fields.OrderBy(field => field.Order)).Include(f => f.Requests)
        );

        if (form is null)
            return Result.Failure<FormDefinitionResponseDto>(FormErrors.FormNotFound);

        return Result.Success(MapToResponseDto(form, form.Requests?.Count ?? 0));
    }

    public async Task<Result<List<FormDefinitionResponseDto>>> GetAllAdminFormsAsync()
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();

        var forms = await formRepo.FindAllAsync(
            f => !f.IsDeleted,
            q => q.Include(f => f.Fields.OrderBy(field => field.Order)).Include(f => f.Requests),
            orderBy: q => q.OrderByDescending(f => f.CreatedAt)
        );

        return Result.Success(forms.Select(f => MapToResponseDto(f, f.Requests?.Count ?? 0)).ToList());
    }

    public async Task<Result<List<FormDefinitionResponseDto>>> GetActiveStudentFormsAsync()
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();
        var now = DateTime.UtcNow;

        var forms = await formRepo.FindAllAsync(
            f => f.IsActive && !f.IsDeleted &&
                 (!f.StartDate.HasValue || f.StartDate.Value <= now) &&
                 (!f.EndDate.HasValue || f.EndDate.Value >= now),
            q => q.Include(f => f.Fields.OrderBy(field => field.Order)),
            orderBy: q => q.OrderByDescending(f => f.CreatedAt)
        );

        return Result.Success(forms.Select(f => MapToResponseDto(f, 0)).ToList());
    }

    public async Task<Result<bool>> ValidateSubmissionAnswersAsync(int formDefinitionId, Dictionary<string, string>? answers)
    {
        var formRepo = _unitOfWork.Repository<FormDefinition>();
        var now = DateTime.UtcNow;

        var form = await formRepo.FindOneAsync(
            f => f.Id == formDefinitionId && !f.IsDeleted,
            q => q.Include(f => f.Fields)
        );

        if (form is null)
            return Result.Failure<bool>(FormErrors.FormNotFound);

        if (!form.IsActive || (form.StartDate.HasValue && form.StartDate.Value > now) || (form.EndDate.HasValue && form.EndDate.Value < now))
            return Result.Failure<bool>(FormErrors.FormClosed);

        foreach (var field in form.Fields)
        {
            var hasValue = answers != null && answers.TryGetValue(field.FieldKey, out var val) && !string.IsNullOrWhiteSpace(val);

            if (field.IsRequired && !hasValue)
            {
                return Result.Failure<bool>(FormErrors.RequiredFieldMissing(field.LabelAr));
            }
        }

        return Result.Success(true);
    }

    private static FormDefinitionResponseDto MapToResponseDto(FormDefinition form, int requestsCount)
    {
        var fieldDtos = form.Fields?.OrderBy(f => f.Order).Select(f =>
        {
            List<string>? options = null;
            if (!string.IsNullOrEmpty(f.OptionsJson))
            {
                try { options = JsonSerializer.Deserialize<List<string>>(f.OptionsJson); } catch { }
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
}
