namespace URMS.Application.DTOs.Requests;

public record ExternalAdministrationResponseDto(
    bool IsApproved,
    string? Notes,
    string? Otp
);