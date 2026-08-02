using URMS.Domain.Enums;

namespace URMS.Application.DTOs.Requests;

public record AdminOverrideRequestDto(
    RequestStatus TargetStatus,
    string? ReasonOrNotes
);
