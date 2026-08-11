using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Common.Pagination;
using URMS.Application.Contracts.Identity;
using URMS.Application.Contracts.Persistence;
using URMS.Application.DTOs.AdvisorAssignment;
using URMS.Domain.Abstractions;
using URMS.Domain.Entities;

namespace URMS.Infrastructure.Identity;

public class AdvisorAssignmentService : IAdvisorAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdvisorAssignmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> BulkAssignAsync(BulkAssignStudentsDto dto)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var studentRepo = _unitOfWork.Repository<Student>();

        // Validate that the advisor exists
        var advisor = await userRepo.FindOneAsync(
            u => u.Id == dto.AdvisorId,
            q => q.AsNoTracking().Include(u => u.Advisor)
        );

        if (advisor is null || advisor.Advisor is null)
            return Result.Failure<int>(new Error("Advisor.NotFound", "Advisor not found.", 404));

        // ─── Batch fetch existing assignments to eliminate N+1 loop ───
        var existingAssignedCodes = (await assignmentRepo.GetQueryable()
            .AsNoTracking()
            .Where(a => dto.UniversityCodes.Contains(a.UniversityCode))
            .Select(a => a.UniversityCode)
            .ToListAsync()).ToHashSet();

        // ─── Batch fetch registered students to eliminate N+1 loop ───
        var registeredStudents = await studentRepo.GetQueryable()
            .Where(s => dto.UniversityCodes.Contains(s.UniversityCode) && s.AcademicAdvisorId == null)
            .ToListAsync();

        var registeredStudentsMap = registeredStudents.ToDictionary(s => s.UniversityCode);

        int addedCount = 0;

        foreach (var code in dto.UniversityCodes)
        {
            if (existingAssignedCodes.Contains(code))
                continue;

            await assignmentRepo.AddAsync(new AdvisorStudentAssignment
            {
                UniversityCode = code,
                AdvisorId = dto.AdvisorId,
                AssignedAt = DateTime.UtcNow
            });

            if (registeredStudentsMap.TryGetValue(code, out var student))
            {
                student.AcademicAdvisorId = dto.AdvisorId;
            }

            addedCount++;
        }

        await _unitOfWork.CompleteAsync();
        return Result.Success(addedCount);
    }

    public async Task<Result<AdvisorAssignmentsGroupDto>> GetAssignmentsByAdvisorAsync(string advisorId, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var studentRepo = _unitOfWork.Repository<Student>();

        var advisor = await userRepo.FindOneAsync(
            u => u.Id == advisorId,
            q => q.AsNoTracking().Include(u => u.Advisor)
        );

        if (advisor is null || advisor.Advisor is null)
            return Result.Failure<AdvisorAssignmentsGroupDto>(new Error("Advisor.NotFound", "Advisor not found.", 404));

        var assignments = await assignmentRepo.GetQueryable()
            .AsNoTracking()
            .Where(a => a.AdvisorId == advisorId)
            .ToListAsync();

        var codes = assignments.Select(a => a.UniversityCode).ToList();

        var registeredStudents = await studentRepo.GetQueryable()
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => codes.Contains(s.UniversityCode))
            .ToDictionaryAsync(s => s.UniversityCode);

        var studentDtos = assignments.Select(a =>
        {
            var isReg = registeredStudents.TryGetValue(a.UniversityCode, out var student);
            return new AssignedStudentDto(
                a.Id,
                a.UniversityCode,
                isReg ? student?.User.FullNameAr : null,
                isReg ? student?.User.FullNameEn : null,
                isReg,
                a.AssignedAt
            );
        }).ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "code" || col == "universitycode")
            {
                studentDtos = studentDtos.Where(s => s.UniversityCode.ToLower().Contains(term)).ToList();
            }
            else if (col == "name")
            {
                studentDtos = studentDtos.Where(s =>
                    (s.StudentNameAr != null && s.StudentNameAr.ToLower().Contains(term)) ||
                    (s.StudentNameEn != null && s.StudentNameEn.ToLower().Contains(term))
                ).ToList();
            }
            else
            {
                studentDtos = studentDtos.Where(s =>
                    s.UniversityCode.ToLower().Contains(term) ||
                    (s.StudentNameAr != null && s.StudentNameAr.ToLower().Contains(term)) ||
                    (s.StudentNameEn != null && s.StudentNameEn.ToLower().Contains(term))
                ).ToList();
            }
        }

        var paginatedStudents = PaginatedList<AssignedStudentDto>.Create(studentDtos, pageNumber, pageSize);

        var groupDto = new AdvisorAssignmentsGroupDto(
            advisor.Id,
            advisor.FullNameAr,
            advisor.FullNameEn,
            advisor.Advisor.AdvisorCode,
            advisor.Email!,
            studentDtos.Count,
            paginatedStudents
        );

        return Result.Success(groupDto);
    }

    public async Task<Result<PaginatedList<AdvisorAssignmentsGroupDto>>> GetAllAssignmentsAsync(string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null)
    {
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var studentRepo = _unitOfWork.Repository<Student>();

        var advisors = await userRepo.GetQueryable()
            .AsNoTracking()
            .Include(u => u.Advisor)
            .Where(u => u.UserType == URMS.Domain.Enums.UserType.AcademicAdvisor)
            .ToListAsync();

        var assignments = await assignmentRepo.GetAllAsync();
        var assignmentsByAdvisor = assignments.GroupBy(a => a.AdvisorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allCodes = assignments.Select(a => a.UniversityCode).Distinct().ToList();

        var registeredStudents = await studentRepo.GetQueryable()
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => allCodes.Contains(s.UniversityCode))
            .ToDictionaryAsync(s => s.UniversityCode);

        var groupDtos = new List<AdvisorAssignmentsGroupDto>();

        foreach (var adv in advisors)
        {
            var advAssignments = assignmentsByAdvisor.GetValueOrDefault(adv.Id, new List<AdvisorStudentAssignment>());

            var studentDtos = advAssignments.Select(a =>
            {
                var isReg = registeredStudents.TryGetValue(a.UniversityCode, out var student);
                return new AssignedStudentDto(
                    a.Id,
                    a.UniversityCode,
                    isReg ? student?.User.FullNameAr : null,
                    isReg ? student?.User.FullNameEn : null,
                    isReg,
                    a.AssignedAt
                );
            }).ToList();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                var matchesAdvisor = adv.FullNameAr.ToLower().Contains(term) ||
                                     adv.FullNameEn.ToLower().Contains(term) ||
                                     (adv.Email != null && adv.Email.ToLower().Contains(term)) ||
                                     (adv.Advisor != null && adv.Advisor.AdvisorCode.ToLower().Contains(term));

                if (!matchesAdvisor)
                {
                    studentDtos = studentDtos.Where(s =>
                        s.UniversityCode.ToLower().Contains(term) ||
                        (s.StudentNameAr != null && s.StudentNameAr.ToLower().Contains(term)) ||
                        (s.StudentNameEn != null && s.StudentNameEn.ToLower().Contains(term))
                    ).ToList();

                    if (!studentDtos.Any())
                        continue;
                }
            }

            var paginatedStudents = PaginatedList<AssignedStudentDto>.Create(studentDtos, 1, 1000);

            groupDtos.Add(new AdvisorAssignmentsGroupDto(
                adv.Id,
                adv.FullNameAr,
                adv.FullNameEn,
                adv.Advisor?.AdvisorCode ?? string.Empty,
                adv.Email ?? string.Empty,
                studentDtos.Count,
                paginatedStudents
            ));
        }

        var paginatedAdvisors = PaginatedList<AdvisorAssignmentsGroupDto>.Create(groupDtos, pageNumber, pageSize);

        return Result.Success(paginatedAdvisors);
    }

    public async Task<Result> RemoveAssignmentAsync(string universityCode)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();

        var assignment = await assignmentRepo.FindOneAsync(a => a.UniversityCode == universityCode);
        if (assignment is null)
            return Result.Failure(new Error("Assignment.NotFound", "Assignment not found for this code.", 404));

        assignmentRepo.Delete(assignment);
        await _unitOfWork.CompleteAsync();
        return Result.Success();
    }

    public async Task<Result> ReassignAsync(AssignStudentDto dto)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var studentRepo = _unitOfWork.Repository<Student>();

        var assignment = await assignmentRepo.FindOneAsync(a => a.UniversityCode == dto.UniversityCode);
        if (assignment is null)
            return Result.Failure(new Error("Assignment.NotFound", "Assignment not found for this code.", 404));

        assignment.AdvisorId = dto.AdvisorId;
        assignment.AssignedAt = DateTime.UtcNow;

        // Update the student too if already registered
        var student = await studentRepo.FindOneAsync(s => s.UniversityCode == dto.UniversityCode);
        if (student is not null)
        {
            student.AcademicAdvisorId = dto.AdvisorId;
        }

        await _unitOfWork.CompleteAsync();
        return Result.Success();
    }

    public async Task<Result<ImportExcelAssignmentsResponseDto>> ImportFromExcelAsync(Stream fileStream)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var studentRepo = _unitOfWork.Repository<Student>();

        // 1. Fetch all Advisors (ApplicationUser where UserType == AcademicAdvisor)
        var advisors = await userRepo.GetQueryable()
            .AsNoTracking()
            .Where(u => u.UserType == URMS.Domain.Enums.UserType.AcademicAdvisor)
            .ToListAsync();

        if (!advisors.Any())
        {
            return Result.Failure<ImportExcelAssignmentsResponseDto>(new Error(
                "Advisor.NoAdvisorsFound",
                "No Academic Advisors found in the database. Please create advisors first.",
                400
            ));
        }

        // Build a normalized lookup dictionary for advisors by Arabic Full Name
        var advisorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var adv in advisors)
        {
            var normName = NormalizeArabicName(adv.FullNameAr);
            if (!string.IsNullOrEmpty(normName) && !advisorMap.ContainsKey(normName))
            {
                advisorMap[normName] = adv.Id;
            }
        }

        // Fetch existing assignments & registered students into memory dictionaries
        var existingAssignments = (await assignmentRepo.GetAllAsync())
            .ToDictionary(a => a.UniversityCode, a => a);

        var registeredStudents = (await studentRepo.GetAllAsync())
            .ToDictionary(s => s.UniversityCode, s => s);

        int totalProcessed = 0;
        int successful = 0;
        int skipped = 0;
        var errors = new List<string>();

        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        int rowIndex = 0;

        while (reader.Read())
        {
            rowIndex++;

            // Skip Header row (Row 1)
            if (rowIndex == 1) continue;

            // Column A (0): Advisor Name ("المرشد")
            // Column B (1): Student Name ("الاسم")
            // Column C (2): Student Code ("كود")
            var advisorNameRaw = reader.GetValue(0)?.ToString()?.Trim();
            var studentNameRaw = reader.GetValue(1)?.ToString()?.Trim();
            var codeRaw = reader.GetValue(2)?.ToString()?.Trim();

            // If Code is missing in Column C (Index 2), but present in Column B (Index 1) as digits
            if (string.IsNullOrWhiteSpace(codeRaw) && !string.IsNullOrWhiteSpace(studentNameRaw) && studentNameRaw.All(char.IsDigit))
            {
                codeRaw = studentNameRaw;
            }

            if (string.IsNullOrWhiteSpace(advisorNameRaw) && string.IsNullOrWhiteSpace(codeRaw))
            {
                // Empty row, skip silently
                continue;
            }

            totalProcessed++;

            if (string.IsNullOrWhiteSpace(advisorNameRaw))
            {
                errors.Add($"الصف {rowIndex}: اسم المرشد غير موجود.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(codeRaw))
            {
                errors.Add($"الصف {rowIndex}: كود الطالب غير موجود.");
                continue;
            }

            var normAdvisorName = NormalizeArabicName(advisorNameRaw);

            if (!advisorMap.TryGetValue(normAdvisorName, out var advisorId))
            {
                // Try fuzzy/partial match if exact normalized match fails
                var matchedAdvisor = advisorMap.FirstOrDefault(kvp => kvp.Key.Contains(normAdvisorName) || normAdvisorName.Contains(kvp.Key));
                if (!string.IsNullOrEmpty(matchedAdvisor.Value))
                {
                    advisorId = matchedAdvisor.Value;
                }
                else
                {
                    errors.Add($"الصف {rowIndex}: لم يتم العثور على المرشد الأكاديمي [{advisorNameRaw}] في النظام.");
                    continue;
                }
            }

            // Assign or update
            if (existingAssignments.TryGetValue(codeRaw, out var existingAssignment))
            {
                if (existingAssignment.AdvisorId != advisorId)
                {
                    existingAssignment.AdvisorId = advisorId;
                    existingAssignment.AssignedAt = DateTime.UtcNow;
                    successful++;
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                var newAssignment = new AdvisorStudentAssignment
                {
                    UniversityCode = codeRaw,
                    AdvisorId = advisorId,
                    AssignedAt = DateTime.UtcNow
                };
                await assignmentRepo.AddAsync(newAssignment);
                existingAssignments[codeRaw] = newAssignment;
                successful++;
            }

            // Also update registered student if present
            if (registeredStudents.TryGetValue(codeRaw, out var student))
            {
                student.AcademicAdvisorId = advisorId;
            }
        }

        await _unitOfWork.CompleteAsync();

        return Result.Success(new ImportExcelAssignmentsResponseDto(
            totalProcessed,
            successful,
            skipped,
            errors
        ));
    }

    public async Task<Result<AdvisorMyStudentsResponseDto>> GetMyStudentsAsync(string advisorUserId, string? searchColumn = null, string? searchTerm = null, int? pageNumber = null, int? pageSize = null)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        var userRepo = _unitOfWork.Repository<ApplicationUser>();
        var studentRepo = _unitOfWork.Repository<Student>();

        var advisor = await userRepo.FindOneAsync(
            u => u.Id == advisorUserId,
            q => q.AsNoTracking().Include(u => u.Advisor)
        );

        if (advisor is null)
            return Result.Failure<AdvisorMyStudentsResponseDto>(new Error("Advisor.NotFound", "Advisor not found.", 404));

        var assignments = await assignmentRepo.GetQueryable()
            .AsNoTracking()
            .Where(a => a.AdvisorId == advisorUserId)
            .ToListAsync();

        var codes = assignments.Select(a => a.UniversityCode).ToList();

        var registeredStudents = await studentRepo.GetQueryable()
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => codes.Contains(s.UniversityCode))
            .ToDictionaryAsync(s => s.UniversityCode);

        var allItems = assignments.Select(a =>
        {
            var isReg = registeredStudents.TryGetValue(a.UniversityCode, out var student);
            return new AdvisorMyStudentItemDto(
                a.Id,
                a.UniversityCode,
                isReg,
                a.AssignedAt,
                isReg ? student?.Id.ToString() : null,
                isReg ? student?.User.FullNameAr : null,
                isReg ? student?.User.FullNameEn : null,
                isReg ? student?.NationalId : null,
                isReg ? student?.User.Email : null,
                isReg ? student?.User.PhoneNumber : null,
                isReg ? student?.User.AlternatePhone : null,
                isReg ? student?.Address : null
            );
        }).ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var col = searchColumn?.Trim().ToLower();

            if (col == "code" || col == "universitycode")
            {
                allItems = allItems.Where(s => s.UniversityCode.ToLower().Contains(term)).ToList();
            }
            else if (col == "name")
            {
                allItems = allItems.Where(s =>
                    (s.FullNameAr != null && s.FullNameAr.ToLower().Contains(term)) ||
                    (s.FullNameEn != null && s.FullNameEn.ToLower().Contains(term))
                ).ToList();
            }
            else if (col == "nationalid")
            {
                allItems = allItems.Where(s => s.NationalId != null && s.NationalId.ToLower().Contains(term)).ToList();
            }
            else if (col == "email")
            {
                allItems = allItems.Where(s => s.Email != null && s.Email.ToLower().Contains(term)).ToList();
            }
            else
            {
                allItems = allItems.Where(s =>
                    s.UniversityCode.ToLower().Contains(term) ||
                    (s.FullNameAr != null && s.FullNameAr.ToLower().Contains(term)) ||
                    (s.FullNameEn != null && s.FullNameEn.ToLower().Contains(term)) ||
                    (s.NationalId != null && s.NationalId.ToLower().Contains(term)) ||
                    (s.Email != null && s.Email.ToLower().Contains(term)) ||
                    (s.PhoneNumber != null && s.PhoneNumber.ToLower().Contains(term))
                ).ToList();
            }
        }

        var registeredCount = allItems.Count(s => s.IsRegistered);
        var unregisteredCount = allItems.Count - registeredCount;

        var paginatedStudents = PaginatedList<AdvisorMyStudentItemDto>.Create(allItems, pageNumber, pageSize);

        var response = new AdvisorMyStudentsResponseDto(
            advisor.Id,
            advisor.FullNameAr,
            advisor.FullNameEn,
            advisor.Advisor?.AdvisorCode ?? string.Empty,
            allItems.Count,
            registeredCount,
            unregisteredCount,
            paginatedStudents
        );

        return Result.Success(response);
    }

    private static string NormalizeArabicName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        return name.Trim()
            .Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا')
            .Replace('ى', 'ي').Replace('ة', 'ه')
            .Replace("  ", " ");
    }
}
