using URMS.Application.DTOs.Auth;

namespace URMS.Application.Contracts.Identity;

public interface IAuthService
{
    Task<UserResponse> LoginAsync(LoginRequest request);
    Task<UserResponse> RegisterStudentAsync(RegisterStudentRequest request);
    Task LogoutAsync();
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<UserResponse?> GetCurrentUserAsync(string userId);
}
