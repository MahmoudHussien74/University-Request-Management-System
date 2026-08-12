using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Common.Helpers;
using URMS.Application.Contracts.Forms;
using URMS.Application.Contracts.Persistence;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Application.Services;

public class RequestCreationService : IRequestCreationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormDefinitionService _formService;

    public RequestCreationService(
        IUnitOfWork unitOfWork,
        IFormDefinitionService formService)
    {
        _unitOfWork = unitOfWork;
        _formService = formService;
    }

    public async Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var student = await userRepo.FindOneAsync(
            u => u.Id == studentId,
            q => q.Include(u => u.Student)
        );

        if (student is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.StudentNotFound);

        if (!student.IsApproved)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.StudentNotApproved);

        // Validate dynamic form rules
        var formValResult = await _formService.ValidateSubmissionAnswersAsync(dto.FormDefinitionId, dto.AdditionalData);
        if (formValResult.IsFailure)
            return Result.Failure<UniversityRequestResponseDto>(formValResult.Error);

        string? jsonMetadata = dto.AdditionalData is not null && dto.AdditionalData.Count > 0
            ? JsonSerializer.Serialize(dto.AdditionalData)
            : null;

        // Auto-assign student's academic advisor to request
        var advisorId = student.Student?.AcademicAdvisorId;
        ApplicationUser? advisor = null;
        if (!string.IsNullOrEmpty(advisorId))
        {
            advisor = await userRepo.GetByIdAsync(advisorId);
        }

        var request = new UniversityRequest
        {
            StudentId = studentId,
            FormDefinitionId = dto.FormDefinitionId,
            Status = RequestStatus.Pending,
            AdditionalDataJson = jsonMetadata,
            AdvisorId = advisorId,
            CreatedAt = DateTime.UtcNow
        };

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = studentId,
            OldStatus = RequestStatus.Pending,
            NewStatus = RequestStatus.Pending,
            ActionMessage = RequestLogMessages.CreatedByStudent
        });

        await requestRepo.AddAsync(request);
        await _unitOfWork.CompleteAsync();

        var formRepo = _unitOfWork.Repository<FormDefinition>();
        request.FormDefinition = await formRepo.GetByIdAsync(request.FormDefinitionId!.Value);

        return Result.Success(request.MapToDto(student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> WithdrawRequestAsync(int requestId, string studentId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.StudentId != studentId)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.Status != RequestStatus.Pending)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForWithdraw);

        var oldStatus = request.Status;
        request.Status = RequestStatus.Rejected;
        request.RejectionReason = "تم سحب الطلب بواسطة الطالب";

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = studentId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = RequestLogMessages.WithdrawnByStudent,
            Notes = request.RejectionReason
        });

        await _unitOfWork.CompleteAsync();
        return Result.Success(request.MapToDto(request.Student, request.Advisor, request.Administration));
    }
}
