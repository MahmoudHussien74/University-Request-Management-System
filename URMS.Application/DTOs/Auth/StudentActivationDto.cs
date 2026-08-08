namespace URMS.Application.DTOs.Auth;

/// <summary>
/// DTO used by admins to view students and manage activation/deactivation.
/// </summary>
public record StudentActivationDto(
    string Id,
    string FullNameAr,
    string FullNameEn,
    string Email,
    string? UniversityCode,
    string? NationalId,
    string? PhoneNumber,
    decimal? GPA,
    bool IsApproved,
    bool IsActive,
    DateTime CreatedAt
);
