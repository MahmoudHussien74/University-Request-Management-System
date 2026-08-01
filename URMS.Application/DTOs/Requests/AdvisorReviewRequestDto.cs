namespace URMS.Application.DTOs.Requests;

public record AdvisorReviewRequestDto(
    bool IsApproved,
    string? RejectionReason
);
