using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Common.Pagination;
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

    public async Task<Result<PaginatedList<PendingStudentDto>>> GetPendingStudentsAsync(
        string callerUserId,
        IList<string> callerRoles,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var isAdvisor = callerRoles.Contains(AppRoles.AcademicAdvisor);

        var query = userRepo.GetQueryable()
            .AsNoTracking()
            .Include(u => u.Student)
            .Where(u => !u.IsApproved && u.IsActive && u.Student != null &&
                        (!isAdvisor || u.Student.AcademicAdvisorId == callerUserId));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "name")
            {
                query = query.Where(u =>
                    u.FirstNameAr.ToLower().Contains(term) || u.LastNameAr.ToLower().Contains(term) ||
                    (u.SecondNameAr != null && u.SecondNameAr.ToLower().Contains(term)) ||
                    (u.ThirdNameAr != null && u.ThirdNameAr.ToLower().Contains(term)) ||
                    u.FirstNameEn.ToLower().Contains(term) || u.LastNameEn.ToLower().Contains(term) ||
                    (u.SecondNameEn != null && u.SecondNameEn.ToLower().Contains(term)) ||
                    (u.ThirdNameEn != null && u.ThirdNameEn.ToLower().Contains(term))
                );
            }
            else if (col == "email")
            {
                query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(term));
            }
            else if (col == "code" || col == "universitycode")
            {
                query = query.Where(u => u.Student != null && u.Student.UniversityCode.ToLower().Contains(term));
            }
            else if (col == "nationalid")
            {
                query = query.Where(u => u.Student != null && u.Student.NationalId.ToLower().Contains(term));
            }
            else
            {
                query = query.Where(u =>
                    u.FirstNameAr.ToLower().Contains(term) || u.LastNameAr.ToLower().Contains(term) ||
                    (u.SecondNameAr != null && u.SecondNameAr.ToLower().Contains(term)) ||
                    (u.ThirdNameAr != null && u.ThirdNameAr.ToLower().Contains(term)) ||
                    u.FirstNameEn.ToLower().Contains(term) || u.LastNameEn.ToLower().Contains(term) ||
                    (u.SecondNameEn != null && u.SecondNameEn.ToLower().Contains(term)) ||
                    (u.ThirdNameEn != null && u.ThirdNameEn.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.Student != null && u.Student.UniversityCode.ToLower().Contains(term)) ||
                    (u.Student != null && u.Student.NationalId.ToLower().Contains(term))
                );
            }
        }

        query = query.OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();

        List<ApplicationUser> pendingUsers;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            pendingUsers = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            pendingUsers = await query.ToListAsync();
        }

        var pendingStudents = pendingUsers.Adapt<List<PendingStudentDto>>();

        var paginatedResult = new PaginatedList<PendingStudentDto>(pendingStudents, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
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

    public async Task<Result<PaginatedList<StudentActivationDto>>> GetAllStudentsForActivationAsync(
        string callerUserId,
        IList<string> callerRoles,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var isAdvisor = callerRoles.Contains(AppRoles.AcademicAdvisor);

        var query = userRepo.GetQueryable()
            .AsNoTracking()
            .Include(u => u.Student)
            .Where(u => u.Student != null &&
                        (!isAdvisor || u.Student.AcademicAdvisorId == callerUserId));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "name")
            {
                query = query.Where(u =>
                    u.FirstNameAr.ToLower().Contains(term) || u.LastNameAr.ToLower().Contains(term) ||
                    (u.SecondNameAr != null && u.SecondNameAr.ToLower().Contains(term)) ||
                    (u.ThirdNameAr != null && u.ThirdNameAr.ToLower().Contains(term)) ||
                    u.FirstNameEn.ToLower().Contains(term) || u.LastNameEn.ToLower().Contains(term) ||
                    (u.SecondNameEn != null && u.SecondNameEn.ToLower().Contains(term)) ||
                    (u.ThirdNameEn != null && u.ThirdNameEn.ToLower().Contains(term))
                );
            }
            else if (col == "email")
            {
                query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(term));
            }
            else if (col == "code" || col == "universitycode")
            {
                query = query.Where(u => u.Student != null && u.Student.UniversityCode.ToLower().Contains(term));
            }
            else if (col == "nationalid")
            {
                query = query.Where(u => u.Student != null && u.Student.NationalId.ToLower().Contains(term));
            }
            else
            {
                query = query.Where(u =>
                    u.FirstNameAr.ToLower().Contains(term) || u.LastNameAr.ToLower().Contains(term) ||
                    (u.SecondNameAr != null && u.SecondNameAr.ToLower().Contains(term)) ||
                    (u.ThirdNameAr != null && u.ThirdNameAr.ToLower().Contains(term)) ||
                    u.FirstNameEn.ToLower().Contains(term) || u.LastNameEn.ToLower().Contains(term) ||
                    (u.SecondNameEn != null && u.SecondNameEn.ToLower().Contains(term)) ||
                    (u.ThirdNameEn != null && u.ThirdNameEn.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.Student != null && u.Student.UniversityCode.ToLower().Contains(term)) ||
                    (u.Student != null && u.Student.NationalId.ToLower().Contains(term))
                );
            }
        }

        query = query.OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();

        List<ApplicationUser> students;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            students = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            students = await query.ToListAsync();
        }

        var result = students.Adapt<List<StudentActivationDto>>();

        var paginatedResult = new PaginatedList<StudentActivationDto>(result, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
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
        var (firstAr, secondAr, thirdAr, lastAr) = SplitFullName(request.FullNameAr);
        user.FirstNameAr = firstAr;
        user.SecondNameAr = secondAr;
        user.ThirdNameAr = thirdAr;
        user.LastNameAr = lastAr;

        var (firstEn, secondEn, thirdEn, lastEn) = SplitFullName(request.FullNameEn);
        user.FirstNameEn = firstEn;
        user.SecondNameEn = secondEn;
        user.ThirdNameEn = thirdEn;
        user.LastNameEn = lastEn;
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

    public async Task<Result<AdvisorDto>> CreateAdvisorAsync(CreateAdvisorDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            return Result.Failure<AdvisorDto>(UserErrors.DuplicateEmail);

        var advisorRepo = _unitOfWork.Repository<AcademicAdvisor>();
        var advisorCode = string.IsNullOrWhiteSpace(dto.AdvisorCode)
            ? $"ADV-{Guid.NewGuid().ToString()[..6].ToUpper()}"
            : dto.AdvisorCode.Trim();

        var existingAdvisorCode = await advisorRepo.FindOneAsync(a => a.AdvisorCode == advisorCode);
        if (existingAdvisorCode is not null)
            return Result.Failure<AdvisorDto>(new Error("Auth.DuplicateAdvisorCode", "كود المرشد الأكاديمي مسجل بالفعل.", 409));

        var (firstAr, secondAr, thirdAr, lastAr) = SplitFullName(dto.FullNameAr);
        var (firstEn, secondEn, thirdEn, lastEn) = SplitFullName(dto.FullNameEn);

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            FirstNameAr = firstAr,
            SecondNameAr = secondAr,
            ThirdNameAr = thirdAr,
            LastNameAr = lastAr,
            FirstNameEn = firstEn,
            SecondNameEn = secondEn,
            ThirdNameEn = thirdEn,
            LastNameEn = lastEn,
            UserType = URMS.Domain.Enums.UserType.AcademicAdvisor,
            IsApproved = true,
            IsActive = true,
            EmailConfirmed = true
        };

        var password = string.IsNullOrWhiteSpace(dto.Password) ? "Advisor@123" : dto.Password;
        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<AdvisorDto>(UserErrors.RegistrationFailed(errors));
        }

        await _userManager.AddToRoleAsync(user, AppRoles.AcademicAdvisor);

        var advisor = new AcademicAdvisor
        {
            UserId = user.Id,
            AdvisorCode = advisorCode
        };

        await advisorRepo.AddAsync(advisor);
        await _unitOfWork.CompleteAsync();

        return Result.Success(new AdvisorDto(
            user.Id,
            user.Email,
            user.FullNameAr,
            user.FullNameEn,
            advisor.AdvisorCode,
            user.PhoneNumber,
            user.IsActive
        ));
    }

    public async Task<Result<BulkCreateAdvisorsResponseDto>> BulkCreateAdvisorsAsync(BulkCreateAdvisorsDto dto)
    {
        if (dto.Advisors is null || !dto.Advisors.Any())
            return Result.Failure<BulkCreateAdvisorsResponseDto>(new Error("Advisor.EmptyList", "قائمة المرشدين فارغة.", 400));

        var createdAdvisors = new List<AdvisorDto>();
        var errors = new List<string>();

        foreach (var item in dto.Advisors)
        {
            var effectiveDto = item with
            {
                Password = string.IsNullOrWhiteSpace(item.Password) ? dto.DefaultPassword : item.Password
            };

            var result = await CreateAdvisorAsync(effectiveDto);
            if (result.IsSuccess)
            {
                createdAdvisors.Add(result.Value);
            }
            else
            {
                errors.Add($"فشل إضافة المرشد [{item.FullNameAr} - {item.Email}]: {result.Error.Message}");
            }
        }

        return Result.Success(new BulkCreateAdvisorsResponseDto(
            createdAdvisors.Count,
            createdAdvisors,
            errors
        ));
    }

    public async Task<Result<PaginatedList<AdvisorDto>>> GetAllAdvisorsAsync(string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();

        var query = userRepo.GetQueryable()
            .AsNoTracking()
            .Include(u => u.Advisor)
            .Where(u => u.Advisor != null && u.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "name")
            {
                query = query.Where(u =>
                    u.FirstNameAr.ToLower().Contains(term) || u.LastNameAr.ToLower().Contains(term) ||
                    (u.SecondNameAr != null && u.SecondNameAr.ToLower().Contains(term)) ||
                    (u.ThirdNameAr != null && u.ThirdNameAr.ToLower().Contains(term)) ||
                    u.FirstNameEn.ToLower().Contains(term) || u.LastNameEn.ToLower().Contains(term) ||
                    (u.SecondNameEn != null && u.SecondNameEn.ToLower().Contains(term)) ||
                    (u.ThirdNameEn != null && u.ThirdNameEn.ToLower().Contains(term))
                );
            }
            else if (col == "email")
            {
                query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(term));
            }
            else if (col == "code" || col == "advisorcode")
            {
                query = query.Where(u => u.Advisor != null && u.Advisor.AdvisorCode.ToLower().Contains(term));
            }
            else
            {
                query = query.Where(u =>
                    u.FirstNameAr.ToLower().Contains(term) || u.LastNameAr.ToLower().Contains(term) ||
                    (u.SecondNameAr != null && u.SecondNameAr.ToLower().Contains(term)) ||
                    (u.ThirdNameAr != null && u.ThirdNameAr.ToLower().Contains(term)) ||
                    u.FirstNameEn.ToLower().Contains(term) || u.LastNameEn.ToLower().Contains(term) ||
                    (u.SecondNameEn != null && u.SecondNameEn.ToLower().Contains(term)) ||
                    (u.ThirdNameEn != null && u.ThirdNameEn.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.Advisor != null && u.Advisor.AdvisorCode.ToLower().Contains(term))
                );
            }
        }

        query = query.OrderBy(u => u.FirstNameAr);

        var totalCount = await query.CountAsync();

        List<ApplicationUser> advisors;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            advisors = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            advisors = await query.ToListAsync();
        }

        var dtos = advisors.Select(u => new AdvisorDto(
            u.Id,
            u.Email!,
            u.FullNameAr,
            u.FullNameEn,
            u.Advisor!.AdvisorCode,
            u.PhoneNumber,
            u.IsActive
        )).ToList();

        var paginatedResult = new PaginatedList<AdvisorDto>(dtos, pageNumber, totalCount, pageSize);

        return Result.Success(paginatedResult);
    }

    private static (string First, string? Second, string? Third, string Last) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return (string.Empty, null, null, string.Empty);

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            0 => (string.Empty, null, null, string.Empty),
            1 => (parts[0], null, null, parts[0]),
            2 => (parts[0], null, null, parts[1]),
            3 => (parts[0], parts[1], null, parts[2]),
            4 => (parts[0], parts[1], parts[2], parts[3]),
            _ => (parts[0], parts[1], parts[2], string.Join(" ", parts[3..]))
        };
    }
}

