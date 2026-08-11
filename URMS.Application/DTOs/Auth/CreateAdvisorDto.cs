namespace URMS.Application.DTOs.Auth;

/// <summary>
/// DTO for creating a single Academic Advisor account.
/// </summary>
public record CreateAdvisorDto(
    string FullNameAr,
    string Email,
    string? FullNameEn = null,
    string? AdvisorCode = null,
    string? PhoneNumber = null,
    string? Password = null
);

/// <summary>
/// DTO for bulk creation of Academic Advisor accounts.
/// </summary>
public record BulkCreateAdvisorsDto(
    List<CreateAdvisorDto> Advisors,
    string? DefaultPassword = null
);

/// <summary>
/// Response DTO containing Academic Advisor info.
/// </summary>
public record AdvisorDto(
    string Id,
    string Email,
    string FullNameAr,
    string FullNameEn,
    string AdvisorCode,
    string? PhoneNumber,
    bool IsActive
);

/// <summary>
/// Response DTO for bulk advisor creation.
/// </summary>
public record BulkCreateAdvisorsResponseDto(
    int TotalCreated,
    List<AdvisorDto> CreatedAdvisors,
    List<string> Errors
);
