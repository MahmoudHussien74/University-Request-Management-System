using URMS.Domain.Common;

namespace URMS.Domain.Entities;

/// <summary>
/// Pre-registration lookup table: maps a UniversityCode to an Advisor
/// BEFORE the student registers. Populated by SuperAdmin from college data.
/// </summary>
public class AdvisorStudentAssignment : BaseEntity
{
    public string UniversityCode { get; set; } = default!;

    public string AdvisorId { get; set; } = default!;
    public ApplicationUser Advisor { get; set; } = default!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
