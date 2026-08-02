using URMS.Domain.Common;

namespace URMS.Domain.Entities;

public class Staff : BaseEntity
{
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
}
