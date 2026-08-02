using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Identity;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequest request);
    Task<Result<UserResponse>> RegisterStudentAsync(RegisterStudentRequest request);
    Task LogoutAsync();
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<Result<UserResponse>> GetCurrentUserAsync(string userId);

    // ─── JWT & Refresh Token Operations ───
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string token, string refreshToken);
    Task<Result> RevokeTokenAsync(string refreshToken);
}
