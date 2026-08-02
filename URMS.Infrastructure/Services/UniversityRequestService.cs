using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Persistence;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Infrastructure.Services;

public class UniversityRequestService : IUniversityRequestService
{
    private readonly IUnitOfWork _unitOfWork;

    public UniversityRequestService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        // Rule check: Extra Hours Registration requires GPA >= 3.0
        if (dto.RequestType == RequestType.ExtraHoursRegistration)
        {
            if (!dto.Gpa.HasValue || dto.Gpa < 3.0m)
                return Result.Failure<UniversityRequestResponseDto>(RequestErrors.GpaTooLow);
        }

        string? jsonMetadata = dto.AdditionalData is not null && dto.AdditionalData.Count > 0
            ? JsonSerializer.Serialize(dto.AdditionalData)
            : null;

        // Auto-assign the student's academic advisor to the request
        var advisorId = student.Student?.AcademicAdvisorId;
        ApplicationUser? advisor = null;
        if (!string.IsNullOrEmpty(advisorId))
        {
            advisor = await userRepo.GetByIdAsync(advisorId);
        }

        var request = new UniversityRequest
        {
            StudentId = studentId,
            Type = dto.RequestType,
            Status = RequestStatus.Pending,
            GPA = dto.Gpa,
            RequestedHours = dto.RequestedHours,
            Notes = dto.Notes,
            AdditionalDataJson = jsonMetadata,
            AdvisorId = advisorId,
            CreatedAt = DateTime.UtcNow
        };

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = studentId,
            OldStatus = RequestStatus.Pending,
            NewStatus = RequestStatus.Pending,
            ActionMessage = RequestLogMessages.CreatedByStudent,
            Notes = dto.Notes
        });

        await requestRepo.AddAsync(request);
        await _unitOfWork.CompleteAsync();

        return Result.Success(MapToDto(request, student, advisor, null));
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetMyRequestsAsync(string studentId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var requests = await requestRepo.FindAllAsync(
            r => r.StudentId == studentId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.Staff)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt)
        );

        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Staff)).ToList());
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetAllRequestsAsync(RequestStatus? status = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var requests = await requestRepo.FindAllAsync(
            r => !status.HasValue || r.Status == status.Value,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.Staff)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt)
        );

        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Staff)).ToList());
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(string advisorId, RequestStatus? status = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var requests = await requestRepo.FindAllAsync(
            r => (r.AdvisorId == advisorId || (r.Student.Student != null && r.Student.Student.AcademicAdvisorId == advisorId))
                 && (!status.HasValue || r.Status == status.Value),
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.Staff)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt)
        );

        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Staff)).ToList());
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.Staff)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        return Result.Success(MapToDto(request, request.Student, request.Advisor, request.Staff));
    }

    public async Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.Status != RequestStatus.Pending && request.Status != RequestStatus.UnderAdvisorReview)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForAdvisorReview);

        request.AdvisorId = advisorId;
        request.AdvisorReviewedAt = DateTime.UtcNow;

        var oldStatus = request.Status;

        if (dto.IsApproved)
        {
            request.Status = RequestStatus.AdvisorApproved;
            request.IsGpaConfirmedByAdvisor = true;
        }
        else
        {
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = dto.RejectionReason;
        }

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = advisorId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = dto.IsApproved ? RequestLogMessages.ApprovedByAdvisor : RequestLogMessages.RejectedByAdvisor,
            Notes = dto.RejectionReason
        });

        await _unitOfWork.CompleteAsync();

        var advisor = await userRepo.GetByIdAsync(advisorId);
        return Result.Success(MapToDto(request, request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> ConfirmByStaffAsync(int requestId, string staffId, StaffConfirmRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        if (request.Status != RequestStatus.AdvisorApproved && request.Status != RequestStatus.SentToStaff)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.InvalidStatusForStaffConfirm);

        request.StaffId = staffId;
        request.StaffConfirmedAt = DateTime.UtcNow;

        var oldStatus = request.Status;

        if (dto.IsApproved)
        {
            request.Status = RequestStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = dto.ConfirmationNotes;
        }

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = staffId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = dto.IsApproved ? RequestLogMessages.ConfirmedByStaff : RequestLogMessages.RejectedByStaff,
            Notes = dto.ConfirmationNotes
        });

        await _unitOfWork.CompleteAsync();

        var staff = await userRepo.GetByIdAsync(staffId);
        return Result.Success(MapToDto(request, request.Student, request.Advisor, staff));
    }

    public async Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.Advisor)
                  .Include(r => r.Staff)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        var oldStatus = request.Status;
        request.Status = dto.TargetStatus;

        if (dto.TargetStatus == RequestStatus.Completed)
        {
            request.CompletedAt = DateTime.UtcNow;
            request.StaffId = adminId;
            request.StaffConfirmedAt = DateTime.UtcNow;

            if (request.AdvisorReviewedAt == null)
            {
                request.AdvisorId = adminId;
                request.AdvisorReviewedAt = DateTime.UtcNow;
                request.IsGpaConfirmedByAdvisor = true;
            }
        }
        else if (dto.TargetStatus == RequestStatus.Rejected)
        {
            request.RejectionReason = dto.ReasonOrNotes;
        }
        else if (dto.TargetStatus == RequestStatus.AdvisorApproved)
        {
            request.AdvisorId = adminId;
            request.AdvisorReviewedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(dto.ReasonOrNotes))
        {
            request.Notes = string.IsNullOrEmpty(request.Notes)
                ? $"[Admin Override]: {dto.ReasonOrNotes}"
                : $"{request.Notes} | [Admin Override]: {dto.ReasonOrNotes}";
        }

        request.HistoryLogs.Add(new RequestHistoryLog
        {
            ActionById = adminId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            ActionMessage = RequestLogMessages.AdminOverride,
            Notes = dto.ReasonOrNotes
        });

        await _unitOfWork.CompleteAsync();

        var adminUser = await userRepo.GetByIdAsync(adminId);
        return Result.Success(MapToDto(request, request.Student, request.Advisor ?? adminUser, request.Staff ?? adminUser));
    }

    private static UniversityRequestResponseDto MapToDto(
        UniversityRequest request,
        ApplicationUser student,
        ApplicationUser? advisor,
        ApplicationUser? staff)
    {
        Dictionary<string, string>? additionalData = null;
        if (!string.IsNullOrEmpty(request.AdditionalDataJson))
        {
            try
            {
                additionalData = JsonSerializer.Deserialize<Dictionary<string, string>>(request.AdditionalDataJson);
            }
            catch
            {
                // Fallback if parsing fails
            }
        }

        List<RequestHistoryLogDto>? historyLogsDto = null;
        if (request.HistoryLogs != null && request.HistoryLogs.Any())
        {
            historyLogsDto = request.HistoryLogs.OrderByDescending(l => l.ActionDate)
                .Select(l => new RequestHistoryLogDto(
                    l.ActionBy?.FullNameAr ?? "System",
                    l.OldStatus.ToString(),
                    l.NewStatus.ToString(),
                    l.ActionMessage,
                    l.Notes,
                    l.ActionDate
                )).ToList();
        }

        var dto = request.Adapt<UniversityRequestResponseDto>();

        return dto with
        {
            StudentNameAr = student.FullNameAr,
            StudentNameEn = student.FullNameEn,
            UniversityCode = student.Student?.UniversityCode,
            AdvisorName = advisor?.FullNameAr,
            StaffName = staff?.FullNameAr,
            AdditionalData = additionalData,
            HistoryLogs = historyLogsDto
        };
    }
}
