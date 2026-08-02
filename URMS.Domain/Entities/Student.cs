using URMS.Domain.Common;

namespace URMS.Domain.Entities;

public class Student : BaseEntity
{
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    public string UniversityCode { get; set; } = default!;
    public string NationalId { get; set; } = default!;
    public string? Address { get; set; }
    public decimal? GPA { get; set; }

    // ─── Academic Advisor (auto-assigned on registration) ───
    public string? AcademicAdvisorId { get; set; }
    public ApplicationUser? AcademicAdvisor { get; set; }
}
