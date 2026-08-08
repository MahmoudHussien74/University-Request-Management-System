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

    public async Task<Result<List<StudentActivationDto>>> GetAllStudentsForActivationAsync()
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var students = await userRepo.FindAllAsync(
            u => u.Student != null,
            q => q.Include(u => u.Student),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt)
        );

        var result = students.Adapt<List<StudentActivationDto>>();

        return Result.Success(result);
    }
}

