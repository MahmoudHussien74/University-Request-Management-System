using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Enums;

namespace URMS.Application.Contracts.Requests;

public interface IUniversityRequestService
{
    Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto);
    List<RequestTypeInfoDto> GetRequestTypes();
    List<RequestStatusInfoDto> GetRequestStatuses();
    Task<Result<List<UniversityRequestResponseDto>>> GetMyRequestsAsync(string studentId);
    Task<Result<List<UniversityRequestResponseDto>>> GetAllRequestsAsync(RequestStatus? status = null);
    Task<Result<List<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(string advisorId, RequestStatus? status = null);
    Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId);
    Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> ConfirmByStaffAsync(int requestId, string staffId, StaffConfirmRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto);
}
