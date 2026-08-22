using URMS.Application.Common.Helpers;

namespace URMS.Application.Services;

public class RequestQueryService : IRequestQueryService
{
    private readonly IUniversityRequestRepository _requestRepo;
    private readonly IRequestAuthorizationService _authService;

    public RequestQueryService(
        IUniversityRequestRepository requestRepo,
        IRequestAuthorizationService authService)
    {
        _requestRepo = requestRepo;
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
        var (items, totalCount) = await _requestRepo.GetRequestsPagedAsync(
            ownershipFilter: r => r.StudentId == studentId,
            status: status,
            searchColumn: searchColumn,
            searchTerm: searchTerm,
            pageNumber: pageNumber,
            pageSize: pageSize);

        var dtos = items.Select(r => r.MapToDto(r.Student, r.Advisor, r.Administration)).ToList();
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
        var (items, totalCount) = await _requestRepo.GetRequestsPagedAsync(
            ownershipFilter: null,
            status: status,
            searchColumn: searchColumn,
            searchTerm: searchTerm,
            pageNumber: pageNumber,
            pageSize: pageSize);

        var dtos = items.Select(r => r.MapToDto(r.Student, r.Advisor, r.Administration)).ToList();
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
        var (items, totalCount) = await _requestRepo.GetRequestsPagedAsync(
            ownershipFilter: r => r.AdvisorId == advisorId || (r.Student.Student != null && r.Student.Student.AcademicAdvisorId == advisorId),
            status: status,
            searchColumn: searchColumn,
            searchTerm: searchTerm,
            pageNumber: pageNumber,
            pageSize: pageSize);

        var dtos = items.Select(r => r.MapToDto(r.Student, r.Advisor, r.Administration)).ToList();
        var paginatedResult = new PaginatedList<UniversityRequestResponseDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId, string callerUserId, IList<string> callerRoles)
    {
        var request = await _requestRepo.GetByIdWithDetailsAsync(requestId);

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
        var request = await _requestRepo.GetByTokenWithDetailsAsync(token);

        if (request is null)
            return Result.Failure<UniversityRequestResponseDto>(RequestErrors.RequestNotFound);

        return Result.Success(request.MapToDto(request.Student, request.Advisor, request.Administration));
    }
}
