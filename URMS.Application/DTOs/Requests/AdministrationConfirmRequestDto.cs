namespace URMS.Application.DTOs.Requests;

public record AdministrationConfirmRequestDto(
    bool IsApproved,
    string? ConfirmationNotes
);
