using URMS.Domain.Constants;
using URMS.Application.Contracts.Requests;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Application.Services;

public class RequestAuthorizationService : IRequestAuthorizationService
{
    public bool CanAccessRequest(UniversityRequest request, string callerUserId, IList<string> callerRoles)
    {
        if (request is null || string.IsNullOrEmpty(callerUserId))
            return false;

        var isSuperAdminOrSecretary = callerRoles.Contains(AppRoles.SuperAdmin) || callerRoles.Contains(AppRoles.CollegeSecretary);
        var isOwnerStudent = request.StudentId == callerUserId;
        var isAssignedAdvisor = request.AdvisorId == callerUserId || request.Student?.Student?.AcademicAdvisorId == callerUserId;

        return isSuperAdminOrSecretary || isOwnerStudent || isAssignedAdvisor;
    }

    public bool CanWithdrawRequest(UniversityRequest request, string studentId)
    {
        if (request is null || string.IsNullOrEmpty(studentId))
            return false;

        return request.StudentId == studentId && request.Status == RequestStatus.Pending;
    }
}
