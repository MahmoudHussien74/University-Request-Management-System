namespace URMS.Application.DTOs.Requests;

public record SendRequestToAdministrationDto(
    string AdministrationEmail,
    string? Message
);