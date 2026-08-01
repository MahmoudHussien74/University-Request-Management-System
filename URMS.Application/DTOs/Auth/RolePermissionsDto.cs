namespace URMS.Application.DTOs.Auth;

public record RolePermissionsDto(
    string RoleName,
    List<string> AssignedPermissions,
    List<string> AllPermissions
);
