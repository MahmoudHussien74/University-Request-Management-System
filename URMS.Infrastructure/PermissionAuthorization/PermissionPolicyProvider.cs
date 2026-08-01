using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace URMS.Infrastructure.PermissionAuthorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null)
            return policy;

        // Dynamically create a policy for permission strings starting with "Permissions."
        if (policyName.StartsWith("Permissions.", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .RequireAuthenticatedUser()
                .Build();
        }

        return null;
    }
}
