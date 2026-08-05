namespace URMS.Application.DTOs.Requests;

public record CreateUniversityRequestDto(
    int FormDefinitionId,
    Dictionary<string, string>? AdditionalData = null
);
