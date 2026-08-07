using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Enums;

namespace URMS.Application.Contracts.Requests;

public interface IUniversityRequestService
{
    Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto);
    List<RequestStatusInfoDto> GetRequestStatuses();
    Task<Result<List<UniversityRequestResponseDto>>> GetMyRequestsAsync(string studentId);
    Task<Result<List<UniversityRequestResponseDto>>> GetAllRequestsAsync(RequestStatus? status = null);
    Task<Result<List<UniversityRequestResponseDto>>> GetAdvisorRequestsAsync(string advisorId, RequestStatus? status = null);
    Task<Result<UniversityRequestResponseDto>> GetRequestByIdAsync(int requestId);
    Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> SendRequestToAdministrationAsync(int requestId, SendRequestToAdministrationDto dto, string advisorId);
    Task<Result<UniversityRequestResponseDto>> GetRequestByTokenAsync(string token);
    Task<Result<UniversityRequestResponseDto>> RespondExternalRequestAsync(string token, ExternalAdministrationResponseDto dto);
    Task<Result<UniversityRequestResponseDto>> ConfirmByAdministrationAsync(int requestId, string administrationId, AdministrationConfirmRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> WithdrawRequestAsync(int requestId, string studentId);
}
