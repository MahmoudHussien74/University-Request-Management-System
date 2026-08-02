using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Constants;
using URMS.Domain.Entities;
using URMS.Domain.Enums;
using URMS.Infrastructure.Persistence;

namespace URMS.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly AppDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IRolePermissionService rolePermissionService,
        IJwtTokenGenerator jwtTokenGenerator,
        AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _rolePermissionService = rolePermissionService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _context = context;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            await _context.Entry(user).Reference(u => u.Student).LoadAsync();
            await _context.Entry(user).Reference(u => u.Advisor).LoadAsync();
            await _context.Entry(user).Reference(u => u.Staff).LoadAsync();
        }

        if (user is null)
            throw new Exception("Invalid email or password.");

        if (!user.IsActive)
            throw new Exception("Account is deactivated.");

        if (!user.IsApproved)
            throw new Exception("Your account is pending approval by your Academic Advisor.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new Exception("Invalid email or password.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<UserResponse> RegisterStudentAsync(RegisterStudentRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new Exception("User with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FirstNameAr = request.FirstNameAr,
            SecondNameAr = request.SecondNameAr,
            ThirdNameAr = request.ThirdNameAr,
            LastNameAr = request.LastNameAr,
            FirstNameEn = request.FirstNameEn,
            SecondNameEn = request.SecondNameEn,
            ThirdNameEn = request.ThirdNameEn,
            LastNameEn = request.LastNameEn,
            AlternatePhone = request.AlternatePhone,
            UserType = UserType.Student,
            IsApproved = false,  // Student registration requires approval from advisor / secretary
            IsActive = true,
            Student = new Student
            {
                UniversityCode = request.UniversityCode,
                NationalId = request.NationalId,
                Address = request.Address
            }
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Student registration failed: {errors}");
        }

        // Assign default Student role
        await _userManager.AddToRoleAsync(user, AppRoles.Student);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rolePermissionService.GetUserPermissionsAsync(user.Id);

        return new UserResponse(
            user.Id,
            user.Email!,
            user.FullNameAr,
            user.FullNameEn,
            user.Student?.UniversityCode,
            null,
            user.IsApproved,
            user.IsActive,
            roles,
            permissions
        );
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new Exception("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Change password failed: {errors}");
        }
    }

    public async Task<UserResponse?> GetCurrentUserAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Advisor)
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rolePermissionService.GetUserPermissionsAsync(user.Id);

        return new UserResponse(
            user.Id,
            user.Email!,
            user.FullNameAr,
            user.FullNameEn,
            user.Student?.UniversityCode,
            user.Advisor?.AdvisorCode,
            user.IsApproved,
            user.IsActive,
            roles,
            permissions
        );
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Advisor)
            .Include(u => u.Staff)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));

        if (user is null)
            throw new Exception("Invalid refresh token.");

        var existingRefreshToken = user.RefreshTokens.Single(t => t.Token == refreshToken);

        if (!existingRefreshToken.IsActive)
            throw new Exception("Refresh token is inactive or expired.");

        // Revoke current refresh token
        existingRefreshToken.IsRevoked = true;
        existingRefreshToken.RevokedOn = DateTime.UtcNow;

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));

        if (user is null) return false;

        var existingRefreshToken = user.RefreshTokens.Single(t => t.Token == refreshToken);

        if (!existingRefreshToken.IsActive) return false;

        existingRefreshToken.IsRevoked = true;
        existingRefreshToken.RevokedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rolePermissionService.GetUserPermissionsAsync(user.Id);

        var (jwtToken, jwtExpiresOn) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponseDto(
            user.Id,
            user.Email!,
            user.FullNameAr,
            user.FullNameEn,
            user.Student?.UniversityCode,
            user.Advisor?.AdvisorCode,
            user.IsApproved,
            user.IsActive,
            roles,
            permissions,
            jwtToken,
            jwtExpiresOn,
            refreshToken.Token,
            refreshToken.ExpiresOn
        );
    }
}
