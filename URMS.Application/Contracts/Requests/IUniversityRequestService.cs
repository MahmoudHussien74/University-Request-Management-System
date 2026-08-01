using URMS.Application.DTOs.Requests;
using URMS.Domain.Enums;

namespace URMS.Application.Contracts.Requests;

public interface IUniversityRequestService
{
    Task<UniversityRequestResponseDto> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto);
    Task<List<UniversityRequestResponseDto>> GetMyRequestsAsync(string studentId);
    Task<List<UniversityRequestResponseDto>> GetAllRequestsAsync(RequestStatus? status = null);
    Task<UniversityRequestResponseDto?> GetRequestByIdAsync(int requestId);
    Task<UniversityRequestResponseDto> ReviewByAdvisorAsync(int requestId, string advisorId, AdvisorReviewRequestDto dto);
    Task<UniversityRequestResponseDto> ConfirmByStaffAsync(int requestId, string staffId, StaffConfirmRequestDto dto);
}
