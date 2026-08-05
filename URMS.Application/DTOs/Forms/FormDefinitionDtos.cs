namespace URMS.Application.DTOs.Forms;

public record CreateFormDefinitionDto(
    string TitleAr,
    string TitleEn,
    string? Description,
    bool IsActive,
    DateTime? StartDate,
    DateTime? EndDate,
    List<CreateFormFieldDto> Fields
);

public record UpdateFormDefinitionDto(
    string TitleAr,
    string TitleEn,
    string? Description,
    bool IsActive,
    DateTime? StartDate,
    DateTime? EndDate,
    List<CreateFormFieldDto> Fields
);

public record ToggleFormStatusDto(
    bool IsActive,
    string? ClosedReasonMessage
);

public record FormDefinitionResponseDto(
    int Id,
    string TitleAr,
    string TitleEn,
    string? Description,
    bool IsActive,
    bool IsDeleted,
    string? ClosedReasonMessage,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime CreatedAt,
    List<FormFieldResponseDto> Fields,
    int RequestsCount
);
