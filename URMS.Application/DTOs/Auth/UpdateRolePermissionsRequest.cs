namespace URMS.Application.DTOs.Auth;

public record UpdateRolePermissionsRequest(
    string RoleName,
    List<string> Permissions
);
