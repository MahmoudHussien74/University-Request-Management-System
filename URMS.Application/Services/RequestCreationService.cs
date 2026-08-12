using URMS.Application.Common.Helpers;

namespace URMS.Application.Services;

public class RequestCreationService : IRequestCreationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUniversityRequestRepository _requestRepo;
    private readonly IFormDefinitionRepository _formRepo;
    private readonly IFormDefinitionService _formService;

    public RequestCreationService(
        IUnitOfWork unitOfWork,
        IUniversityRequestRepository requestRepo,
        IFormDefinitionRepository formRepo,
        IFormDefinitionService formService)
    {
        _unitOfWork = unitOfWork;
        _requestRepo = requestRepo;
        _formRepo = formRepo;
        _formService = formService;
    }

    public async Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto)
    {
        var student = await _requestRepo.GetStudentWithProfileAsync(studentId);

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
            advisor = await _requestRepo.GetUserByIdAsync(advisorId);
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

        await _requestRepo.AddAsync(request);
        await _unitOfWork.CompleteAsync();

        request.FormDefinition = await _formRepo.GetByIdAsync(request.FormDefinitionId!.Value);

        return Result.Success(request.MapToDto(student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> WithdrawRequestAsync(int requestId, string studentId)
    {
        var request = await _requestRepo.GetByIdWithDetailsAsync(requestId);

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
