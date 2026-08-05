using URMS.Domain.Common;

namespace URMS.Domain.Entities;

public class FormDefinition : AuditableEntity
{
    public string TitleAr { get; set; } = default!;
    public string TitleEn { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public string? ClosedReasonMessage { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public ICollection<FormFieldDefinition> Fields { get; set; } = new List<FormFieldDefinition>();
    public ICollection<UniversityRequest> Requests { get; set; } = new List<UniversityRequest>();
}
