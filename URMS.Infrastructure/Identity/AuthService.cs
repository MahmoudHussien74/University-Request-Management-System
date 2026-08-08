using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Identity;
using URMS.Application.Contracts.Persistence;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IRolePermissionService rolePermissionService,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _rolePermissionService = rolePermissionService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequest request)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var user = await userRepo.FindOneAsync(
            u => u.Email == request.Email,
            q => q.Include(u => u.Student).Include(u => u.Advisor).Include(u => u.Staff)
        );

        if (user is null)
            return Result.Failure<AuthResponseDto>(UserErrors.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<AuthResponseDto>(UserErrors.AccountDeactivated);

        if (!user.IsApproved)
            return Result.Failure<AuthResponseDto>(UserErrors.AccountNotApproved);

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return Result.Failure<AuthResponseDto>(UserErrors.InvalidCredentials);

        return Result.Success(await GenerateAuthResponseAsync(user));
    }

    public async Task<Result<UserResponse>> RegisterStudentAsync(RegisterStudentRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Result.Failure<UserResponse>(UserErrors.DuplicateEmail);

        var studentRepo = _unitOfWork.Repository<Student>();

        var existingUniversityCode = await studentRepo.FindOneAsync(
            s => s.UniversityCode == request.UniversityCode
        );
        if (existingUniversityCode is not null)
            return Result.Failure<UserResponse>(UserErrors.DuplicateUniversityCode);

        var existingNationalId = await studentRepo.FindOneAsync(
            s => s.NationalId == request.NationalId
        );
        if (existingNationalId is not null)
            return Result.Failure<UserResponse>(UserErrors.DuplicateNationalId);

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
            return Result.Failure<UserResponse>(UserErrors.RegistrationFailed(errors));
        }

        // Assign default Student role
        await _userManager.AddToRoleAsync(user, AppRoles.Student);

        // ─── Auto-link to Academic Advisor from pre-registration lookup ───
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var assignment = await assignmentRepo.FindOneAsync(
            a => a.UniversityCode == request.UniversityCode
        );

        if (assignment is not null)
        {
            user.Student!.AcademicAdvisorId = assignment.AdvisorId;
            await _unitOfWork.CompleteAsync();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rolePermissionService.GetUserPermissionsAsync(user.Id);

        return Result.Success(new UserResponse(
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
        ));
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(UserErrors.ChangePasswordFailed(errors));
        }

        return Result.Success();
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(string userId)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var user = await userRepo.FindOneAsync(
            u => u.Id == userId,
            q => q.Include(u => u.Student).Include(u => u.Advisor).Include(u => u.Staff)
        );

        if (user is null)
            return Result.Failure<UserResponse>(UserErrors.UserNotFound);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rolePermissionService.GetUserPermissionsAsync(user.Id);

        return Result.Success(new UserResponse(
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
        ));
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string token, string refreshToken)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var user = await userRepo.FindOneAsync(
            u => u.RefreshTokens.Any(t => t.Token == refreshToken),
            q => q.Include(u => u.Student).Include(u => u.Advisor).Include(u => u.Staff).Include(u => u.RefreshTokens)
        );

        if (user is null)
            return Result.Failure<AuthResponseDto>(UserErrors.InvalidRefreshToken);

        var existingRefreshToken = user.RefreshTokens.Single(t => t.Token == refreshToken);

        if (!existingRefreshToken.IsActive)
            return Result.Failure<AuthResponseDto>(UserErrors.RefreshTokenInactive);

        // Revoke current refresh token
        existingRefreshToken.IsRevoked = true;
        existingRefreshToken.RevokedOn = DateTime.UtcNow;

        return Result.Success(await GenerateAuthResponseAsync(user));
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var user = await userRepo.FindOneAsync(
            u => u.RefreshTokens.Any(t => t.Token == refreshToken),
            q => q.Include(u => u.RefreshTokens)
        );

        if (user is null)
            return Result.Failure(UserErrors.InvalidRefreshToken);

        var existingRefreshToken = user.RefreshTokens.Single(t => t.Token == refreshToken);

        if (!existingRefreshToken.IsActive)
            return Result.Failure(UserErrors.RefreshTokenInactive);

        existingRefreshToken.IsRevoked = true;
        existingRefreshToken.RevokedOn = DateTime.UtcNow;

        await _unitOfWork.CompleteAsync();
        return Result.Success();
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _rolePermissionService.GetUserPermissionsAsync(user.Id);

        var (jwtToken, jwtExpiresOn) = _jwtTokenGenerator.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);
        await _unitOfWork.CompleteAsync();

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
