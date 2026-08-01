using Microsoft.AspNetCore.Identity;

namespace URMS.Domain.Entities;

/// <summary>
/// Extended Identity user representing all system users (Students, Advisors, Staff, etc.)
/// </summary>
public class ApplicationUser : IdentityUser
{
    // ─── Arabic Name (4 parts — required for Students, optional for others) ───
    public string FirstNameAr { get; set; } = default!;
    public string? SecondNameAr { get; set; }
    public string? ThirdNameAr { get; set; }
    public string LastNameAr { get; set; } = default!;

    // ─── English Name (4 parts — required for Students, optional for others) ───
    public string FirstNameEn { get; set; } = default!;
    public string? SecondNameEn { get; set; }
    public string? ThirdNameEn { get; set; }
    public string LastNameEn { get; set; } = default!;

    // ─── Student-Specific Fields ───
    public string? UniversityCode { get; set; }
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public decimal? GPA { get; set; }

    // ─── Advisor-Specific Fields ───
    public string? AdvisorCode { get; set; }
    public string? AvailabilityDays { get; set; }   // Stored as comma-separated: "Sunday,Monday,Wednesday"
    public string? PendingAvailabilityDays { get; set; }  // Pending change awaiting approval

    // ─── Common Fields ───
    public string? AlternatePhone { get; set; }
    public bool IsApproved { get; set; }            // Student registration requires approval
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ─── Computed Properties ───
    public string FullNameAr =>
        string.Join(" ", new[] { FirstNameAr, SecondNameAr, ThirdNameAr, LastNameAr }
            .Where(n => !string.IsNullOrWhiteSpace(n)));

    public string FullNameEn =>
        string.Join(" ", new[] { FirstNameEn, SecondNameEn, ThirdNameEn, LastNameEn }
            .Where(n => !string.IsNullOrWhiteSpace(n)));

}
