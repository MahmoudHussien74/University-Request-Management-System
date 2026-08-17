using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Identity;

public interface IAuthService
{
    Task<Result<AuthResult>> LoginAsync(LoginRequest request);
    Task<Result<UserResponse>> RegisterStudentAsync(RegisterStudentRequest request);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<Result<UserResponse>> GetCurrentUserAsync(string userId);

    // ─── JWT & Refresh Token Operations ───
    Task<Result<AuthResult>> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<Result> RevokeTokenAsync(string refreshToken);
}
