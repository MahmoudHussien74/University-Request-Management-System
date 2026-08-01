using Microsoft.AspNetCore.Authorization;

namespace URMS.Infrastructure.PermissionAuthorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
    }
}
