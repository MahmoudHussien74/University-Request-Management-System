namespace URMS.Application.DTOs.Auth;

public record RegisterStudentRequest(
    string FirstNameAr,
    string SecondNameAr,
    string ThirdNameAr,
    string LastNameAr,
    string FirstNameEn,
    string SecondNameEn,
    string ThirdNameEn,
    string LastNameEn,
    string UniversityCode,
    string NationalId,
    string Email,
    string PhoneNumber,
    string? AlternatePhone,
    string Address,
    string Password,
    string ConfirmPassword
);
