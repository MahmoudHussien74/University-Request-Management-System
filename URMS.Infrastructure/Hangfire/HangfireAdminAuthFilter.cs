using Hangfire;
using Hangfire.Dashboard;

namespace URMS.Infrastructure.Hangfire;

/// <summary>
/// Restricts Hangfire Dashboard access to authenticated SuperAdmin users only.
/// </summary>
public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("SuperAdmin");
    }
}
