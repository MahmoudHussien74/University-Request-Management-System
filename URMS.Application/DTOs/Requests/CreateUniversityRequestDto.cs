using URMS.Domain.Enums;

namespace URMS.Application.DTOs.Requests;

public record CreateUniversityRequestDto(
    RequestType RequestType,
    decimal GPA,
    int RequestedHours,
    string? Notes
);
