using URMS.Domain.Common;
using URMS.Domain.Enums;

namespace URMS.Domain.Entities;

public class FormFieldDefinition : BaseEntity
{
    public int FormDefinitionId { get; set; }
    public FormDefinition FormDefinition { get; set; } = default!;

    public string FieldKey { get; set; } = default!;   // e.g. "courseCode"
    public string LabelAr { get; set; } = default!;    // e.g. "كود المقرر"
    public string LabelEn { get; set; } = default!;    // e.g. "Course Code"
    public string? Placeholder { get; set; }

    public FieldType Type { get; set; }
    public bool IsRequired { get; set; } = false;
    public int Order { get; set; } = 0;

    /// <summary>
    /// JSON Array string of options for Dropdown/Radio/Checkbox (e.g. ["عذر طبي", "ظروف شخصية"])
    /// </summary>
    public string? OptionsJson { get; set; }
}
