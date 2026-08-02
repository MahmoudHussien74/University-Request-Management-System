using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Constants;
using URMS.Domain.Entities;
using URMS.Infrastructure.Persistence;

namespace URMS.Infrastructure.Identity;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IRolePermissionService _rolePermissionService;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        IRolePermissionService rolePermissionService)
    {
        _userManager = userManager;
        _context = context;
        _rolePermissionService = rolePermissionService;
    }

    public async Task<List<PendingStudentDto>> GetPendingStudentsAsync()
    {
        var pendingUsers = await _context.Users
            .Include(u => u.Student)
            .Where(u => !u.IsApproved && u.IsActive && u.Student != null)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var pendingStudents = pendingUsers.Select(u => new PendingStudentDto(
            u.Id,
            u.FullNameAr,
            u.FullNameEn,
            u.Email!,
            u.Student!.UniversityCode,
            u.Student.NationalId,
            u.PhoneNumber,
            u.Student.Address,
            u.CreatedAt
        )).ToList();

        return pendingStudents;
    }

    public async Task<UserResponse> ApproveStudentAsync(string studentId)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Advisor)
            .FirstOrDefaultAsync(u => u.Id == studentId);

        if (user is null)
            throw new Exception("Student not found.");

        if (user.IsApproved)
            throw new Exception("Student is already approved.");

        user.IsApproved = true;
        await _userManager.UpdateAsync(user);

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

    public async Task DeactivateAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new Exception("User not found.");

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
    }

    public async Task ReactivateAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new Exception("User not found.");

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
    }
}
