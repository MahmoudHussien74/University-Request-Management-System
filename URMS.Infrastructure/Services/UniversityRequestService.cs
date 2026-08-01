using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Entities;
using URMS.Domain.Enums;
using URMS.Infrastructure.Persistence;

namespace URMS.Infrastructure.Services;

public class UniversityRequestService : IUniversityRequestService
{
    private readonly AppDbContext _context;

    public UniversityRequestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UniversityRequestResponseDto> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto)
    {
        var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
        if (student is null)
            throw new Exception("Student not found.");

        if (!student.IsApproved)
            throw new Exception("Your student account is pending approval by advisor/secretary.");

        // Rule check: Extra Hours Registration requires GPA >= 3.0
        if (dto.RequestType == RequestType.ExtraHoursRegistration && dto.GPA < 3.0m)
        {
            throw new Exception("Extra hours registration requires a minimum GPA of 3.00.");
        }

        var request = new UniversityRequest
        {
            StudentId = studentId,
            Type = dto.RequestType,
            Status = RequestStatus.Pending,
            GPA = dto.GPA,
            RequestedHours = dto.RequestedHours,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.UniversityRequests.Add(request);
        await _context.SaveChangesAsync();

        return MapToDto(request, student, null, null);
    }

    public async Task<List<UniversityRequestResponseDto>> GetMyRequestsAsync(string studentId)
    {
        var requests = await _context.UniversityRequests
            .Include(r => r.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Staff)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Staff)).ToList();
    }

    public async Task<List<UniversityRequestResponseDto>> GetAllRequestsAsync(RequestStatus? status = null)
    {
        var query = _context.UniversityRequests
            .Include(r => r.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Staff)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return requests.Select(r => MapToDto(r, r.Student, r.Advisor, r.Staff)).ToList();
    }

    public async Task<UniversityRequestResponseDto?> GetRequestByIdAsync(int requestId)
    {
        var request = await _context.UniversityRequests
            .Include(r => r.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Staff)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null) return null;

        return MapToDto(request, request.Student, request.Advisor, request.Staff);
    }

    public async Task<UniversityRequestResponseDto> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto)
    {
        var request = await _context.UniversityRequests
            .Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            throw new Exception("Request not found.");

        if (request.Status != RequestStatus.Pending && request.Status != RequestStatus.UnderAdvisorReview)
            throw new Exception($"Cannot review request in '{request.Status}' status.");

        request.AdvisorId = advisorId;
        request.AdvisorReviewedAt = DateTime.UtcNow;

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

        await _context.SaveChangesAsync();

        var advisor = await _context.Users.FindAsync(advisorId);
        return MapToDto(request, request.Student, advisor, null);
    }

    public async Task<UniversityRequestResponseDto> ConfirmByStaffAsync(int requestId, string staffId, StaffConfirmRequestDto dto)
    {
        var request = await _context.UniversityRequests
            .Include(r => r.Student)
            .Include(r => r.Advisor)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            throw new Exception("Request not found.");

        if (request.Status != RequestStatus.AdvisorApproved && request.Status != RequestStatus.SentToStaff)
            throw new Exception($"Cannot process staff confirmation for request in '{request.Status}' status.");

        request.StaffId = staffId;
        request.StaffConfirmedAt = DateTime.UtcNow;

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

        await _context.SaveChangesAsync();

        var staff = await _context.Users.FindAsync(staffId);
        return MapToDto(request, request.Student, request.Advisor, staff);
    }

    private static UniversityRequestResponseDto MapToDto(
        UniversityRequest request,
        ApplicationUser student,
        ApplicationUser? advisor,
        ApplicationUser? staff)
    {
        return new UniversityRequestResponseDto(
            request.Id,
            request.StudentId,
            student.FullNameAr,
            student.FullNameEn,
            student.UniversityCode,
            request.Type,
            request.Status,
            request.GPA,
            request.RequestedHours ?? 0,
            request.Notes,
            request.AdvisorId,
            advisor?.FullNameAr,
            request.RejectionReason,
            request.StaffId,
            staff?.FullNameAr,
            request.CreatedAt,
            request.AdvisorReviewedAt,
            request.CompletedAt
        );
    }
}
