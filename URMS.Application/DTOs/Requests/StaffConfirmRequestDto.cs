namespace URMS.Application.DTOs.Requests;

public record StaffConfirmRequestDto(
    bool IsApproved,
    string? ConfirmationNotes
);
