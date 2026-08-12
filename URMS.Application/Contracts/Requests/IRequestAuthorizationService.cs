using URMS.Domain.Entities;

namespace URMS.Application.Contracts.Requests;

public interface IRequestAuthorizationService
{
    bool CanAccessRequest(UniversityRequest request, string callerUserId, IList<string> callerRoles);
    bool CanWithdrawRequest(UniversityRequest request, string studentId);
}
