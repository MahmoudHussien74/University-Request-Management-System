using URMS.Application.Common.Pagination;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Enums;

namespace URMS.Application.Contracts.Requests;

public interface IRequestQueryService
{
    List<RequestStatusInfoDto> GetRequestStatuses();
    Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetMyRequestsAsync(string studentId, RequestStatus? status = null, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
    Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAllRequestsAsync(RequestStatus? status = null, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
    Task<Result<PaginatedList<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(string advisorId, RequestStatus? status = null, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
    Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId, string callerUserId, IList<string> callerRoles);
    Task<Result<UniversityRequestResponseDto>> GetRequestByTokenAsync(string token);
}
