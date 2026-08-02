using Microsoft.AspNetCore.Identity;
using URMS.Domain.Enums;

namespace URMS.Domain.Entities;

/// <summary>
/// Core Identity user representing authentication credentials and shared personal info across all system roles.
/// Role-specific domain data is decoupled into 1-to-1 entities (Student, AcademicAdvisor, Staff).
/// </summary>
public class ApplicationUser : IdentityUser
{
    // ─── Arabic Name (4 parts) ───
    public string FirstNameAr { get; set; } = default!;
    public string? SecondNameAr { get; set; }
    public string? ThirdNameAr { get; set; }
    public string LastNameAr { get; set; } = default!;

    // ─── English Name (4 parts) ───
    public string FirstNameEn { get; set; } = default!;
    public string? SecondNameEn { get; set; }
    public string? ThirdNameEn { get; set; }
    public string LastNameEn { get; set; } = default!;

    // ─── Common Metadata ───
    public string? AlternatePhone { get; set; }
    public UserType UserType { get; set; }
    public bool IsApproved { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ─── Computed Properties ───
    public string FullNameAr =>
        string.Join(" ", new[] { FirstNameAr, SecondNameAr, ThirdNameAr, LastNameAr }
            .Where(n => !string.IsNullOrWhiteSpace(n)));

    public string FullNameEn =>
        string.Join(" ", new[] { FirstNameEn, SecondNameEn, ThirdNameEn, LastNameEn }
            .Where(n => !string.IsNullOrWhiteSpace(n)));

    // ─── 1-to-1 Entity Navigation Properties ───
    public Student? Student { get; set; }
    public AcademicAdvisor? Advisor { get; set; }
    public Staff? Staff { get; set; }

    // ─── Refresh Tokens Navigation ───
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
