using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Constants;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IRolePermissionService _rolePermissionService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IRolePermissionService rolePermissionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _rolePermissionService = rolePermissionService;
    }

    public async Task<UserResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new Exception("Invalid email or password.");

        if (!user.IsActive)
            throw new Exception("Account is deactivated.");

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
            throw new Exception("Invalid email or password.");

        return await MapToUserResponseAsync(user);
    }

    public async Task<UserResponse> RegisterStudentAsync(RegisterStudentRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new Exception("Password and confirm password do not match.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new Exception("User with this email already exists.");

        var student = new ApplicationUser
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
            UniversityCode = request.UniversityCode,
            NationalId = request.NationalId,
            AlternatePhone = request.AlternatePhone,
            Address = request.Address,
            IsApproved = false,  // Student registration requires approval from advisor / secretary
            IsActive = true
        };

        var result = await _userManager.CreateAsync(student, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Student registration failed: {errors}");
        }

        // Assign default Student role
        await _userManager.AddToRoleAsync(student, AppRoles.Student);

        return await MapToUserResponseAsync(student);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            throw new Exception("New password and confirm password do not match.");

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
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        return await MapToUserResponseAsync(user);
    }

    private async Task<UserResponse> MapToUserResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rolePermissionService.GetUserPermissionsAsync(user.Id);

        return new UserResponse(
            user.Id,
            user.Email!,
            user.FullNameAr,
            user.FullNameEn,
            user.UniversityCode,
            user.AdvisorCode,
            user.IsApproved,
            user.IsActive,
            roles,
            permissions
        );
    }
}
