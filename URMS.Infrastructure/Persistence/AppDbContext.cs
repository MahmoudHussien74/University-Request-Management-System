using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<AcademicAdvisor> AcademicAdvisors => Set<AcademicAdvisor>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<UniversityRequest> UniversityRequests => Set<UniversityRequest>();
    public DbSet<RequestHistoryLog> RequestHistoryLogs => Set<RequestHistoryLog>();
    public DbSet<AvailabilityChangeRequest> AvailabilityChangeRequests => Set<AvailabilityChangeRequest>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
