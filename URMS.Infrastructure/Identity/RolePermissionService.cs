using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Constants;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Identity;

public class RolePermissionService : IRolePermissionService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolePermissionService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<List<RolePermissionsDto>> GetAllRolesWithPermissionsAsync()
    {
        var roles = _roleManager.Roles.ToList();
        var allPermissions = Permissions.GetAllPermissions().ToList();
        var result = new List<RolePermissionsDto>();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var assignedPermissions = claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToList();

            result.Add(new RolePermissionsDto(role.Name!, assignedPermissions, allPermissions));
        }

        return result;
    }

    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null) return null;

        var claims = await _roleManager.GetClaimsAsync(role);
        var assignedPermissions = claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToList();

        var allPermissions = Permissions.GetAllPermissions().ToList();
        return new RolePermissionsDto(role.Name!, assignedPermissions, allPermissions);
    }

    public async Task UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request)
    {
        var role = await _roleManager.FindByNameAsync(request.RoleName);
        if (role is null)
            throw new Exception($"Role '{request.RoleName}' not found.");

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();

        // Remove all existing permission claims
        foreach (var claim in permissionClaims)
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        // Add requested permission claims
        foreach (var permission in request.Permissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return [];

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = new HashSet<string>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is not null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims.Where(c => c.Type == "Permission"))
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        return permissions.ToList();
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permission);
    }
}
