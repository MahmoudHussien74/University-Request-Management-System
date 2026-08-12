using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Entities;

namespace URMS.Application.Contracts.Requests;

public interface IRequestNotificationService
{
    Task<Result> SendExternalAdministrationEmailAsync(
        UniversityRequest request,
        SendRequestToAdministrationDto dto,
        string otpCode,
        string reviewLink);
}
