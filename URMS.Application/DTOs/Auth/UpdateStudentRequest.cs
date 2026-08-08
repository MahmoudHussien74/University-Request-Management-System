namespace URMS.Application.DTOs.Auth;

public record UpdateStudentRequest(
    string FullNameAr,
    string FullNameEn,
    string UniversityCode,
    string NationalId,
    string Email,
    string PhoneNumber,
    string? AlternatePhone,
    string? Address
);
