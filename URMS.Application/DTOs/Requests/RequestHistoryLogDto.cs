namespace URMS.Application.DTOs.Requests;

public record RequestHistoryLogDto(
    string ActionByName,
    string OldStatusName,
    string NewStatusName,
    string ActionMessage,
    DateTime ActionDate,
    string ActionByNameAr,
    string ActionByNameEn,
    string OldStatusNameAr,
    string OldStatusNameEn,
    string NewStatusNameAr,
    string NewStatusNameEn,
    string ActionMessageAr,
    string ActionMessageEn,
    string? Notes = null
);
