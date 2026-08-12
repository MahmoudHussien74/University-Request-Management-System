using Microsoft.EntityFrameworkCore;
using URMS.Application.Common.Helpers;
using URMS.Application.Common.Pagination;
using URMS.Application.Contracts.Persistence;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Application.Services;

public class RequestQueryService : IRequestQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestAuthorizationService _authService;

    public RequestQueryService(
        IUnitOfWork unitOfWork,
        IRequestAuthorizationService authService)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
    }

    public List<RequestStatusInfoDto> GetRequestStatuses()
    {
        return new List<RequestStatusInfoDto>
        {
            new(0, RequestStatus.Pending.ToString(), "معلق (في انتظار مراجعة المرشد)", "Pending Advisor Review"),
            new(1, RequestStatus.AdvisorApproved.ToString(), "موافق عليه من المرشد الأكاديمي", "Approved by Advisor"),
            new(2, RequestStatus.SentToAdministration.ToString(), "أُرسِل إلى شؤون الطلاب / الإدارة", "Sent to Administration"),
            new(3, RequestStatus.Completed.ToString(), "مكتمل / تم التنفيذ", "Completed"),
            new(4, RequestStatus.Rejected.ToString(), "مرفوض", "Rejected")
        };
    }

    public async Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetMyRequestsAsync(
        string studentId,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        IQueryable<UniversityRequest> query = requestRepo.GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .Where(r => r.StudentId == studentId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "title")
            {
                query = query.Where(r => r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)));
            }
            else if (col == "reason" || col == "rejectionreason")
            {
                query = query.Where(r => r.RejectionReason != null && r.RejectionReason.ToLower().Contains(term));
            }
            else
            {
                query = query.Where(r =>
                    (r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term))) ||
                    (r.RejectionReason != null && r.RejectionReason.ToLower().Contains(term))
                );
            }
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        List<UniversityRequest> requests;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            requests = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            requests = await query.ToListAsync();
        }

        var dtos = requests.Select(r => r.MapToDto(r.Student, r.Advisor, r.Administration)).ToList();

        var paginatedResult = new PaginatedList<UniversityRequestResponseDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAllRequestsAsync(
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        IQueryable<UniversityRequest> query = requestRepo.GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "title")
            {
                query = query.Where(r => r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)));
            }
            else if (col == "code" || col == "universitycode")
            {
                query = query.Where(r => r.Student != null && r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term));
            }
            else if (col == "studentname" || col == "name")
            {
                query = query.Where(r => r.Student != null && (
                    r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                    (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                    r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                    (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term))
                ));
            }
            else if (col == "advisorname")
            {
                query = query.Where(r => r.Advisor != null && (
                    r.Advisor.FirstNameAr.ToLower().Contains(term) || r.Advisor.LastNameAr.ToLower().Contains(term) ||
                    (r.Advisor.SecondNameAr != null && r.Advisor.SecondNameAr.ToLower().Contains(term)) ||
                    (r.Advisor.ThirdNameAr != null && r.Advisor.ThirdNameAr.ToLower().Contains(term)) ||
                    r.Advisor.FirstNameEn.ToLower().Contains(term) || r.Advisor.LastNameEn.ToLower().Contains(term) ||
                    (r.Advisor.SecondNameEn != null && r.Advisor.SecondNameEn.ToLower().Contains(term)) ||
                    (r.Advisor.ThirdNameEn != null && r.Advisor.ThirdNameEn.ToLower().Contains(term))
                ));
            }
            else
            {
                query = query.Where(r =>
                    (r.Student != null && (
                        r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                        (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                        r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                        (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term)) ||
                        (r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term))
                    )) ||
                    (r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term))) ||
                    (r.Advisor != null && (
                        r.Advisor.FirstNameAr.ToLower().Contains(term) || r.Advisor.LastNameAr.ToLower().Contains(term) ||
                        (r.Advisor.SecondNameAr != null && r.Advisor.SecondNameAr.ToLower().Contains(term)) ||
                        (r.Advisor.ThirdNameAr != null && r.Advisor.ThirdNameAr.ToLower().Contains(term)) ||
                        r.Advisor.FirstNameEn.ToLower().Contains(term) || r.Advisor.LastNameEn.ToLower().Contains(term) ||
                        (r.Advisor.SecondNameEn != null && r.Advisor.SecondNameEn.ToLower().Contains(term)) ||
                        (r.Advisor.ThirdNameEn != null && r.Advisor.ThirdNameEn.ToLower().Contains(term))
                    ))
                );
            }
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        List<UniversityRequest> requests;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            requests = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            requests = await query.ToListAsync();
        }

        var dtos = requests.Select(r => r.MapToDto(r.Student, r.Advisor, r.Administration)).ToList();

        var paginatedResult = new PaginatedList<UniversityRequestResponseDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(
        string advisorId,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        IQueryable<UniversityRequest> query = requestRepo.GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .Where(r => r.AdvisorId == advisorId || (r.Student.Student != null && r.Student.Student.AcademicAdvisorId == advisorId));

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "title")
            {
                query = query.Where(r => r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)));
            }
            else if (col == "code" || col == "universitycode")
            {
                query = query.Where(r => r.Student != null && r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term));
            }
            else if (col == "studentname" || col == "name")
            {
                query = query.Where(r => r.Student != null && (
                    r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                    (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                    r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                    (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                    (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term))
                ));
            }
            else
            {
                query = query.Where(r =>
                    (r.Student != null && (
                        r.Student.FirstNameAr.ToLower().Contains(term) || r.Student.LastNameAr.ToLower().Contains(term) ||
                        (r.Student.SecondNameAr != null && r.Student.SecondNameAr.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameAr != null && r.Student.ThirdNameAr.ToLower().Contains(term)) ||
                        r.Student.FirstNameEn.ToLower().Contains(term) || r.Student.LastNameEn.ToLower().Contains(term) ||
                        (r.Student.SecondNameEn != null && r.Student.SecondNameEn.ToLower().Contains(term)) ||
                        (r.Student.ThirdNameEn != null && r.Student.ThirdNameEn.ToLower().Contains(term)) ||
                        (r.Student.Student != null && r.Student.Student.UniversityCode.ToLower().Contains(term))
                    )) ||
                    (r.FormDefinition != null && (r.FormDefinition.TitleAr.ToLower().Contains(term) || r.FormDefinition.TitleEn.ToLower().Contains(term)))
                );
            }
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        List<UniversityRequest> requests;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            requests = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            requests = await query.ToListAsync();
        }

        var dtos = requests.Select(r => r.MapToDto(r.Student, r.Advisor, r.Administration)).ToList();

        var paginatedResult = new PaginatedList<UniversityRequestResponseDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId, string callerUserId, IList<string> callerRoles)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var request = await requestRepo.FindOneAsync(
            r => r.Id == requestId,
            q => q.AsNoTracking()
                  .AsSplitQuery()
                  .Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.Administration)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        // IDOR Protection / Ownership & Access Check
        if (!_authService.CanAccessRequest(request, callerUserId, callerRoles))
        {
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.UnauthorizedAccess);
        }

        return Result.Success(request.MapToDto(request.Student!, request.Advisor, request.Administration));
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByTokenAsync(string token)
    {
        var requestRepo = _unitOfWork.Repository<UniversityRequest>();

        var request = await requestRepo.FindOneAsync(
            r => r.ConfirmationToken == token,
            q => q.Include(r => r.Student).ThenInclude(u => u.Student)
                  .Include(r => r.FormDefinition)
                  .Include(r => r.Advisor)
                  .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
        );

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        return Result.Success(request.MapToDto(request.Student, request.Advisor, request.Administration));
    }
}
