using URMS.Application.DTOs.Auth;

namespace URMS.Application.Contracts.Identity;

public interface IRolePermissionService
{
    Task<List<RolePermissionsDto>> GetAllRolesWithPermissionsAsync();
    Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleName);
    Task<Result> UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request);
    Task<List<string>> GetUserPermissionsAsync(string userId);
    Task<bool> HasPermissionAsync(string userId, string permission);
}
