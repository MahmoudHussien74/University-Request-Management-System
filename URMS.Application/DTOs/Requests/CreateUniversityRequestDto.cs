using URMS.Domain.Enums;

namespace URMS.Application.DTOs.Requests;

public record CreateUniversityRequestDto(
    RequestType RequestType,
    decimal? Gpa,
    int? RequestedHours,
    string? Notes,
    Dictionary<string, string>? AdditionalData
);
