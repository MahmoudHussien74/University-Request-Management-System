using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Requests;

public interface IRequestWorkflowService
{
    Task<Result<UniversityRequestResponseDto>> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> SendRequestToAdministrationAsync(int requestId, SendRequestToAdministrationDto dto, string advisorId);
    Task<Result<UniversityRequestResponseDto>> RespondExternalRequestAsync(string token, ExternalAdministrationResponseDto dto);
    Task<Result<UniversityRequestResponseDto>> ConfirmByAdministrationAsync(int requestId, string administrationId, AdministrationConfirmRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> OverrideStatusByAdminAsync(int requestId, string adminId, AdminOverrideRequestDto dto);
}
