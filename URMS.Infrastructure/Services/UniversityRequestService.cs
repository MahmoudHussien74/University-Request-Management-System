using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Entities;
using URMS.Domain.Enums;
using URMS.Domain.Constants;
using URMS.Infrastructure.Persistence;

namespace URMS.Infrastructure.Services;

public class UniversityRequestService : IUniversityRequestService
{
    private readonly AppDbContext _context;

    public UniversityRequestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto)
    {
        var student = await _context.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.Id == studentId);

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

        var request = new UniversityRequest
        {
            StudentId = studentId,
            Type = dto.RequestType,
            Status = RequestStatus.Pending,
            GPA = dto.Gpa,
            RequestedHours = dto.RequestedHours,
            Notes = dto.Notes,
            AdditionalDataJson = jsonMetadata,
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

        _context.UniversityRequests.Add(request);
        await _context.SaveChangesAsync();

        return Result.Success(MapToDto(request, student, null, null));
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetMyRequestsAsync(string studentId)
    {
        var requests = await _context.UniversityRequests
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Staff)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Staff)).ToList());
    }

    public async Task<Result<List<UniversityRequestResponseDto>>> GetAllRequestsAsync(RequestStatus? status = null)
    {
        var query = _context.UniversityRequests
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Staff)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Result.Success(requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Staff)).ToList());
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId)
    {
        var request = await _context.UniversityRequests
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Staff)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        return Result.Success(MapToDto(request, request.Student, request.Advisor, request.Staff));
    }

    public async Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto)
    {
        var request = await _context.UniversityRequests
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);

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

        await _context.SaveChangesAsync();

        var advisor = await _context.Users.FindAsync(advisorId);
        return Result.Success(MapToDto(request, request.Student, advisor, null));
    }

    public async Task<Result<UniversityRequestResponseDto>> ConfirmByStaffAsync(int requestId, string staffId, StaffConfirmRequestDto dto)
    {
        var request = await _context.UniversityRequests
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.Advisor)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);

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

        await _context.SaveChangesAsync();

        var staff = await _context.Users.FindAsync(staffId);
        return Result.Success(MapToDto(request, request.Student, request.Advisor, staff));
    }

    public async Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto)
    {
        var request = await _context.UniversityRequests
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Staff)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        var oldStatus = request.Status;
        request.Status = dto.TargetStatus;

        if (dto.TargetStatus == RequestStatus.Completed)
        {
            request.CompletedAt = DateTime.UtcNow;
            request.StaffId = adminId;
            request.StaffConfirmedAt = DateTime.UtcNow;
            
            // If it bypassed the advisor phase, implicitly set the advisor approval to now.
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

        await _context.SaveChangesAsync();

        var adminUser = await _context.Users.FindAsync(adminId);
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

        return new UniversityRequestResponseDto(
            request.Id,
            request.StudentId,
            student.FullNameAr,
            student.FullNameEn,
            student.Student?.UniversityCode,
            request.Type.ToString(),
            request.Status.ToString(),
            request.GPA,
            request.RequestedHours,
            request.Notes,
            additionalData,
            request.AdvisorId,
            advisor?.FullNameAr,
            request.RejectionReason,
            request.StaffId,
            staff?.FullNameAr,
            request.CreatedAt,
            request.AdvisorReviewedAt,
            request.CompletedAt,
            historyLogsDto
        );
    }
}
