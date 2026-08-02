namespace URMS.Application.DTOs.Requests;

public record RequestHistoryLogDto(
    string ActionByName,
    string OldStatusName,
    string NewStatusName,
    string ActionMessage,
    string? Notes,
    DateTime ActionDate
);
