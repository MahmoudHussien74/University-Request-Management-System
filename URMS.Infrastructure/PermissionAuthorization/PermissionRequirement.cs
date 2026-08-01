using Microsoft.AspNetCore.Authorization;

namespace URMS.Infrastructure.PermissionAuthorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
