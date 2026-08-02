namespace URMS.Application.DTOs.Auth;

public record PendingStudentDto(
    string Id,
    string FullNameAr,
    string FullNameEn,
    string Email,
    string? UniversityCode,
    string? NationalId,
    string? PhoneNumber,
    string? Address,
    DateTime CreatedAt
);
