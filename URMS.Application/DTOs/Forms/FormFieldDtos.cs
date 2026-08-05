using URMS.Domain.Enums;

namespace URMS.Application.DTOs.Forms;

public record CreateFormFieldDto(
    string FieldKey,
    string LabelAr,
    string LabelEn,
    string? Placeholder,
    FieldType Type,
    bool IsRequired,
    int Order,
    List<string>? Options
);

public record FormFieldResponseDto(
    int Id,
    string FieldKey,
    string LabelAr,
    string LabelEn,
    string? Placeholder,
    string Type,
    bool IsRequired,
    int Order,
    List<string>? Options
);
