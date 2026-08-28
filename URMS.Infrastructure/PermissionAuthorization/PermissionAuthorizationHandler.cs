using Microsoft.AspNetCore.Authorization;

namespace URMS.Infrastructure.PermissionAuthorization;

/// <summary>
/// Reads permissions directly from JWT claims instead of querying the database.
/// Permissions are already embedded in the token by JwtTokenGenerator (claim type: "Permission").
/// This eliminates 3-5 DB round-trips per authorized request.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.HasClaim("Permission", requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
