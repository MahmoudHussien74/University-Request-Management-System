using System.Security.Claims;

namespace URMS.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public static List<string> GetUserRoles(this ClaimsPrincipal user) =>
        user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}
