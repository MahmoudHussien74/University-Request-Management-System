using Microsoft.AspNetCore.Authorization;

namespace URMS.Api.Authorization;

/// <summary>
/// Custom authorization attribute that maps a permission string to a dynamic ASP.NET Core policy.
/// Works with PermissionPolicyProvider and PermissionAuthorizationHandler in Infrastructure.
/// 
/// Moved to API layer to eliminate direct API → Infrastructure coupling.
/// The connection is through policy NAME strings, not code references.
/// </summary>
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
    }
}
