using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Identity;
using URMS.Application.Contracts.Persistence;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Identity;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRolePermissionService _rolePermissionService;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        IRolePermissionService rolePermissionService)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _rolePermissionService = rolePermissionService;
    }

    public async Task<Result<List<PendingStudentDto>>> GetPendingStudentsAsync()
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var pendingUsers = await userRepo.FindAllAsync(
            u => !u.IsApproved && u.IsActive && u.Student != null,
            q => q.Include(u => u.Student),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt)
        );

        var pendingStudents = pendingUsers.Adapt<List<PendingStudentDto>>();

        return Result.Success(pendingStudents);
    }

    public async Task<Result<UserResponse>> ApproveStudentAsync(string studentId)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var user = await userRepo.FindOneAsync(
            u => u.Id == studentId,
            q => q.Include(u => u.Student).Include(u => u.Advisor)
        );

        if (user is null)
            return Result.Failure<UserResponse>(UserErrors.UserNotFound);

        if (user.IsApproved)
            return Result.Failure<UserResponse>(UserErrors.StudentAlreadyApproved);

        user.IsApproved = true;
        await _userManager.UpdateAsync(user);

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

    public async Task<Result> DeactivateAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> ReactivateAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result<List<StudentActivationDto>>> GetAllStudentsForActivationAsync(string callerUserId, IList<string> callerRoles)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var isAdvisor = callerRoles.Contains(AppRoles.AcademicAdvisor);

        var students = await userRepo.FindAllAsync(
            u => u.Student != null &&
                 (!isAdvisor || u.Student.AcademicAdvisorId == callerUserId),
            q => q.Include(u => u.Student),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt)
        );

        var result = students.Adapt<List<StudentActivationDto>>();

        return Result.Success(result);
    }

    public async Task<Result> UpdateStudentAsync(string userId, UpdateStudentRequest request)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var user = await userRepo.FindOneAsync(
            u => u.Id == userId,
            q => q.Include(u => u.Student)
        );

        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        // ─── Uniqueness checks (exclude current user) ───

        // Email
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingEmail = await userRepo.FindOneAsync(u => u.Email == request.Email && u.Id != userId);
            if (existingEmail is not null)
                return Result.Failure(UserErrors.DuplicateEmail);
        }

        // UniversityCode
        if (user.Student is not null &&
            !string.Equals(user.Student.UniversityCode, request.UniversityCode, StringComparison.OrdinalIgnoreCase))
        {
            var studentRepo = _unitOfWork.Repository<Student>();
            var existingCode = await studentRepo.FindOneAsync(s => s.UniversityCode == request.UniversityCode && s.UserId != userId);
            if (existingCode is not null)
                return Result.Failure(UserErrors.DuplicateUniversityCode);
        }

        // NationalId
        if (user.Student is not null &&
            !string.Equals(user.Student.NationalId, request.NationalId, StringComparison.OrdinalIgnoreCase))
        {
            var studentRepo = _unitOfWork.Repository<Student>();
            var existingNationalId = await studentRepo.FindOneAsync(s => s.NationalId == request.NationalId && s.UserId != userId);
            if (existingNationalId is not null)
                return Result.Failure(UserErrors.DuplicateNationalId);
        }

        // ─── Split full names into parts (First, Second, Third, Last) ───
        var arParts = request.FullNameAr.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        user.FirstNameAr = arParts[0];
        user.SecondNameAr = arParts.Length > 2 ? arParts[1] : null;
        user.ThirdNameAr = arParts.Length > 3 ? arParts[2] : null;
        user.LastNameAr = arParts.Length switch
        {
            1 => arParts[0],
            2 => arParts[1],
            3 => arParts[2],
            _ => string.Join(" ", arParts[3..])  // Join remaining parts as last name
        };
        if (arParts.Length > 3) user.ThirdNameAr = arParts[2];

        var enParts = request.FullNameEn.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        user.FirstNameEn = enParts[0];
        user.SecondNameEn = enParts.Length > 2 ? enParts[1] : null;
        user.ThirdNameEn = enParts.Length > 3 ? enParts[2] : null;
        user.LastNameEn = enParts.Length switch
        {
            1 => enParts[0],
            2 => enParts[1],
            3 => enParts[2],
            _ => string.Join(" ", enParts[3..])
        };
        if (enParts.Length > 3) user.ThirdNameEn = enParts[2];
        user.Email = request.Email;
        user.UserName = request.Email;
        user.NormalizedEmail = request.Email.ToUpperInvariant();
        user.NormalizedUserName = request.Email.ToUpperInvariant();
        user.PhoneNumber = request.PhoneNumber;
        user.AlternatePhone = request.AlternatePhone;

        // ─── Update Student-specific fields ───
        if (user.Student is not null)
        {
            user.Student.UniversityCode = request.UniversityCode;
            user.Student.NationalId = request.NationalId;
            user.Student.Address = request.Address;
        }

        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
}

