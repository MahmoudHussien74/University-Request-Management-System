namespace URMS.Domain.Constants;

public static class AppRoles
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string Student = nameof(Student);
    public const string AcademicAdvisor = nameof(AcademicAdvisor);
    public const string CollegeSecretary = nameof(CollegeSecretary);

    /// <summary>
    /// All 4 system roles defined for URMS.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        Student,
        AcademicAdvisor,
        CollegeSecretary
    ];
}
