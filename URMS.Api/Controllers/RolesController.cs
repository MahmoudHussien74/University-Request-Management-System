using Microsoft.AspNetCore.Mvc;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Constants;
using URMS.Infrastructure.PermissionAuthorization;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissionService;

    public RolesController(IRolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    /// <summary>
    /// Get all roles with their assigned permissions.
    /// </summary>
    [HttpGet("permissions")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<List<RolePermissionsDto>>> GetAllRolesWithPermissions()
    {
        var result = await _rolePermissionService.GetAllRolesWithPermissionsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get permission details for a specific role.
    /// </summary>
    [HttpGet("{roleName}/permissions")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<RolePermissionsDto>> GetRolePermissions(string roleName)
    {
        var result = await _rolePermissionService.GetRolePermissionsAsync(roleName);
        if (result is null)
            return NotFound($"Role '{roleName}' not found.");

        return Ok(result);
    }

    /// <summary>
    /// Update assigned permissions for a role.
    /// </summary>
    [HttpPut("permissions")]
    [HasPermission(Permissions.Roles.ManagePermissions)]
    public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsRequest request)
    {
        await _rolePermissionService.UpdateRolePermissionsAsync(request);
        return Ok(new { message = $"Permissions for role '{request.RoleName}' updated successfully." });
    }
}
