using URMS.Application.Common.Pagination;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Enums;

namespace URMS.Application.Services;

public class UniversityRequestService : IUniversityRequestService
{
    private readonly IRequestCreationService _creationService;
    private readonly IRequestWorkflowService _workflowService;
    private readonly IRequestQueryService _queryService;

    public UniversityRequestService(
        IRequestCreationService creationService,
        IRequestWorkflowService workflowService,
        IRequestQueryService queryService)
    {
        _creationService = creationService;
        _workflowService = workflowService;
        _queryService = queryService;
    }

    public Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto) =>
        _creationService.CreateRequestAsync(studentId, dto);

    public Task<Result<UniversityRequestResponseDto>> WithdrawRequestAsync(int requestId, string studentId) =>
        _creationService.WithdrawRequestAsync(requestId, studentId);

    public List<RequestStatusInfoDto> GetRequestStatuses() =>
        _queryService.GetRequestStatuses();

    public Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetMyRequestsAsync(
        string studentId,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null) =>
        _queryService.GetMyRequestsAsync(studentId, status, searchColumn, searchTerm, pageNumber, pageSize);

    public Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAllRequestsAsync(
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null) =>
        _queryService.GetAllRequestsAsync(status, searchColumn, searchTerm, pageNumber, pageSize);

    public Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(
        string advisorId,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null) =>
        _queryService.GetAdvisorRequestsAsync(advisorId, status, searchColumn, searchTerm, pageNumber, pageSize);

    public Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId, string callerUserId, IList<string> callerRoles) =>
        _queryService.GetRequestByIdAsync(requestId, callerUserId, callerRoles);

    public Task<Result<UniversityRequestResponseDto>> GetRequestByTokenAsync(string token) =>
        _queryService.GetRequestByTokenAsync(token);

    public Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto) =>
        _workflowService.ReviewByAdvisorAsync(requestId, advisorId, dto);

    public Task<Result<UniversityRequestResponseDto>> SendRequestToAdministrationAsync(int requestId, SendRequestToAdministrationDto dto, string advisorId) =>
        _workflowService.SendRequestToAdministrationAsync(requestId, dto, advisorId);

    public Task<Result<UniversityRequestResponseDto>> RespondExternalRequestAsync(string token, ExternalAdministrationResponseDto dto) =>
        _workflowService.RespondExternalRequestAsync(token, dto);

    public Task<Result<UniversityRequestResponseDto>> ConfirmByAdministrationAsync(int requestId, string administrationId, AdministrationConfirmRequestDto dto) =>
        _workflowService.ConfirmByAdministrationAsync(requestId, administrationId, dto);

    public Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto) =>
        _workflowService.OverrideStatusByAdminAsync(requestId, adminId, dto);
}
