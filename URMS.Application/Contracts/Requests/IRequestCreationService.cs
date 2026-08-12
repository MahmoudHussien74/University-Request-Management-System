using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Requests;

public interface IRequestCreationService
{
    Task<Result<UniversityRequestResponseDto>> CreateRequestAsync(string studentId, CreateUniversityRequestDto dto);
    Task<Result<UniversityRequestResponseDto>> WithdrawRequestAsync(int requestId, string studentId);
}
