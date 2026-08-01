namespace URMS.Application.DTOs.Auth;

public record UserResponse(
    string Id,
    string Email,
    string FullNameAr,
    string FullNameEn,
    string? UniversityCode,
    string? AdvisorCode,
    bool IsApproved,
    bool IsActive,
    IList<string> Roles,
    IList<string> Permissions
);
