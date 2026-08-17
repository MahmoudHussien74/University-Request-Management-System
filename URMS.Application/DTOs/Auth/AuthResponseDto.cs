namespace URMS.Application.DTOs.Auth;
public record AuthResponseDto(
    string Id,
    string Email,
    string FullNameAr,
    string FullNameEn,
    string? UniversityCode,
    string? AdvisorCode,
    bool IsApproved,
    bool IsActive
);