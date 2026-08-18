using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Common.Helpers;
using URMS.Application.Common.Pagination;
using URMS.Application.Contracts.Identity;
using URMS.Application.Contracts.Persistence;
using URMS.Application.DTOs.AdvisorAssignment;
using URMS.Domain.Abstractions;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Infrastructure.Identity;

public class AdvisorAssignmentService : IAdvisorAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public AdvisorAssignmentService(IUnitOfWork unitOfWork, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands (Write Operations)
    // ═══════════════════════════════════════════════════════════════

    public async Task<Result<int>> BulkAssignAsync(
        BulkAssignStudentsDto dto,
        CancellationToken cancellationToken = default)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();

        // Validate advisor exists
        var advisorExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == dto.AdvisorId && u.Advisor != null, cancellationToken);

        if (!advisorExists)
            return Result.Failure<int>(new Error("Advisor.NotFound", "Advisor not found.", 404));

        // Batch fetch existing assignments to eliminate N+1
        var existingAssignedCodes = await _context.AdvisorStudentAssignments
            .AsNoTracking()
            .Where(a => dto.UniversityCodes.Contains(a.UniversityCode))
            .Select(a => a.UniversityCode)
            .ToListAsync(cancellationToken);

        var existingSet = existingAssignedCodes.ToHashSet();

        // Batch fetch registered students (only unassigned) for advisor linking
        var registeredStudents = await _context.Students
            .Where(s => dto.UniversityCodes.Contains(s.UniversityCode) && s.AcademicAdvisorId == null)
            .ToListAsync(cancellationToken);

        var registeredStudentsMap = registeredStudents.ToDictionary(s => s.UniversityCode);

        int addedCount = 0;

        foreach (var code in dto.UniversityCodes)
        {
            if (existingSet.Contains(code))
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

    public async Task<Result> RemoveAssignmentAsync(
        string universityCode,
        CancellationToken cancellationToken = default)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();

        var assignment = await assignmentRepo.FindOneAsync(a => a.UniversityCode == universityCode);
        if (assignment is null)
            return Result.Failure(new Error("Assignment.NotFound", "Assignment not found for this code.", 404));

        assignmentRepo.Delete(assignment);
        await _unitOfWork.CompleteAsync();
        return Result.Success();
    }

    public async Task<Result> ReassignAsync(
        AssignStudentDto dto,
        CancellationToken cancellationToken = default)
    {
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();

        var assignment = await assignmentRepo.FindOneAsync(a => a.UniversityCode == dto.UniversityCode);
        if (assignment is null)
            return Result.Failure(new Error("Assignment.NotFound", "Assignment not found for this code.", 404));

        assignment.AdvisorId = dto.AdvisorId;
        assignment.AssignedAt = DateTime.UtcNow;

        // Update the student too if already registered
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UniversityCode == dto.UniversityCode, cancellationToken);

        if (student is not null)
        {
            student.AcademicAdvisorId = dto.AdvisorId;
        }

        await _unitOfWork.CompleteAsync();
        return Result.Success();
    }

    // ═══════════════════════════════════════════════════════════════
    // Queries (Read Operations) — All use IQueryable SQL Pipeline
    // ═══════════════════════════════════════════════════════════════

    public async Task<Result<AdvisorAssignmentsGroupDto>> GetAssignmentsByAdvisorAsync(
        string advisorId, string? searchColumn = null, string? searchTerm = null,
        int? pageNumber = null, int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate advisor
        var advisor = await _context.Users
            .AsNoTracking()
            .Include(u => u.Advisor)
            .FirstOrDefaultAsync(u => u.Id == advisorId && u.Advisor != null, cancellationToken);

        if (advisor is null)
            return Result.Failure<AdvisorAssignmentsGroupDto>(
                new Error("Advisor.NotFound", "Advisor not found.", 404));

        // 2. Build IQueryable with LEFT JOIN — everything translates to SQL
        var query = BuildAssignedStudentQuery()
            .Where(dto => dto.AdvisorId == advisorId);

        // 3. Apply dynamic search at SQL level
        query = query.ApplySearch(searchColumn, searchTerm);

        // 4. Count + Paginate at SQL level
        query = query.OrderByDescending(x => x.AssignedAt);
        var (projections, totalCount, pNum, pSize) = await PaginateAsync(query, pageNumber, pageSize, cancellationToken);
        var items = projections.Select(x => x.ToDto()).ToList();

        var paginated = new PaginatedList<AssignedStudentDto>(items, pNum, totalCount, pSize);

        return Result.Success(new AdvisorAssignmentsGroupDto(
            advisor.Id, advisor.FullNameAr, advisor.FullNameEn,
            advisor.Advisor!.AdvisorCode, advisor.Email ?? string.Empty,
            totalCount, paginated));
    }

    public async Task<Result<AdvisorMyStudentsResponseDto>> GetMyStudentsAsync(
        string advisorUserId, string? searchColumn = null, string? searchTerm = null,
        int? pageNumber = null, int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate advisor
        var advisor = await _context.Users
            .AsNoTracking()
            .Include(u => u.Advisor)
            .FirstOrDefaultAsync(u => u.Id == advisorUserId, cancellationToken);

        if (advisor is null)
            return Result.Failure<AdvisorMyStudentsResponseDto>(
                new Error("Advisor.NotFound", "Advisor not found.", 404));

        // 2. Build IQueryable with LEFT JOIN
        var query = BuildMyStudentQuery()
            .Where(dto => dto.AdvisorId == advisorUserId);

        // 3. Apply dynamic search at SQL level
        query = query.ApplySearch(searchColumn, searchTerm);

        // 4. Counts at SQL level
        var totalCount = await query.CountAsync(cancellationToken);
        var registeredCount = await query.CountAsync(x => x.IsRegistered, cancellationToken);

        // 5. Paginate at SQL level
        var (pNum, pSize) = NormalizePagination(pageNumber, pageSize, totalCount);

        var projections = await query
            .OrderByDescending(x => x.AssignedAt)
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        var items = projections.Select(x => x.ToDto()).ToList();
        var paginated = new PaginatedList<AdvisorMyStudentItemDto>(items, pNum, totalCount, pSize);

        return Result.Success(new AdvisorMyStudentsResponseDto(
            advisor.Id, advisor.FullNameAr, advisor.FullNameEn,
            advisor.Advisor?.AdvisorCode ?? string.Empty,
            totalCount, registeredCount, totalCount - registeredCount, paginated));
    }

    public async Task<Result<PaginatedList<AdvisorAssignmentsGroupDto>>> GetAllAssignmentsAsync(
        string? searchColumn = null, string? searchTerm = null,
        int? pageNumber = null, int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Build advisor query
        var advisorQuery = _context.Users
            .AsNoTracking()
            .Include(u => u.Advisor)
            .Where(u => u.UserType == UserType.AcademicAdvisor);

        // 2. Apply advisor-level search at SQL level
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            advisorQuery = advisorQuery.Where(u =>
                u.FirstNameAr.Contains(term) || u.LastNameAr.Contains(term) ||
                u.FirstNameEn.Contains(term) || u.LastNameEn.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.Advisor != null && u.Advisor.AdvisorCode.Contains(term)));
        }

        // 3. Paginate advisors at SQL level
        var totalAdvisors = await advisorQuery.CountAsync(cancellationToken);
        var (pNum, pSize) = NormalizePagination(pageNumber, pageSize, totalAdvisors);

        var advisors = await advisorQuery
            .OrderBy(u => u.FirstNameAr)
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        // 4. Batch query: assignments for only this page of advisors (not entire DB)
        var advisorIds = advisors.Select(a => a.Id).ToList();

        var assignmentData = await (
            from a in _context.AdvisorStudentAssignments.AsNoTracking()
            where advisorIds.Contains(a.AdvisorId)
            from s in _context.Students
                .Where(s => s.UniversityCode == a.UniversityCode)
                .DefaultIfEmpty()
            orderby a.AssignedAt descending
            select new
            {
                a.AdvisorId,
                Dto = new AssignedStudentDto(
                    a.Id,
                    a.UniversityCode,
                    s != null ? s.User.FirstNameAr + " " + s.User.LastNameAr : null,
                    s != null ? s.User.FirstNameEn + " " + s.User.LastNameEn : null,
                    s != null,
                    a.AssignedAt)
            }
        ).ToListAsync(cancellationToken);

        var byAdvisor = assignmentData
            .GroupBy(x => x.AdvisorId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

        // 5. Build grouped response
        var groupDtos = advisors.Select(adv =>
        {
            var students = byAdvisor.GetValueOrDefault(adv.Id, []);
            var paginated = PaginatedList<AssignedStudentDto>.Create(students, 1, 1000);

            return new AdvisorAssignmentsGroupDto(
                adv.Id, adv.FullNameAr, adv.FullNameEn,
                adv.Advisor?.AdvisorCode ?? string.Empty,
                adv.Email ?? string.Empty,
                students.Count, paginated);
        }).ToList();

        var result = new PaginatedList<AdvisorAssignmentsGroupDto>(
            groupDtos, pNum, totalAdvisors, pSize);

        return Result.Success(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Excel Import
    // ═══════════════════════════════════════════════════════════════

    public async Task<Result<ImportExcelAssignmentsResponseDto>> ImportFromExcelAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 1. Fetch advisors (small bounded table — safe to load)
        var advisors = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserType == UserType.AcademicAdvisor)
            .Select(u => new { u.Id, u.FirstNameAr, u.SecondNameAr, u.ThirdNameAr, u.LastNameAr })
            .ToListAsync(cancellationToken);

        if (!advisors.Any())
        {
            return Result.Failure<ImportExcelAssignmentsResponseDto>(new Error(
                "Advisor.NoAdvisorsFound",
                "No Academic Advisors found in the database. Please create advisors first.",
                400));
        }

        // Build normalized lookup (exact match only — no fuzzy matching)
        var advisorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var adv in advisors)
        {
            var fullName = string.Join(" ", new[] { adv.FirstNameAr, adv.SecondNameAr, adv.ThirdNameAr, adv.LastNameAr }
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var normName = NormalizeArabicName(fullName);
            advisorMap.TryAdd(normName, adv.Id);
        }

        // 2. Read all rows from Excel (single pass)
        var rows = new List<(int RowIndex, string AdvisorName, string Code)>();
        var errors = new List<string>();

        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        int rowIndex = 0;

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            rowIndex++;
            if (rowIndex == 1) continue; // Skip header

            var advisorNameRaw = reader.GetValue(0)?.ToString()?.Trim();
            var studentNameRaw = reader.GetValue(1)?.ToString()?.Trim();
            var codeRaw = reader.GetValue(2)?.ToString()?.Trim();

            // Handle code in wrong column
            if (string.IsNullOrWhiteSpace(codeRaw) && !string.IsNullOrWhiteSpace(studentNameRaw)
                && studentNameRaw.All(char.IsDigit))
            {
                codeRaw = studentNameRaw;
            }

            // Skip empty rows
            if (string.IsNullOrWhiteSpace(advisorNameRaw) && string.IsNullOrWhiteSpace(codeRaw))
                continue;

            if (string.IsNullOrWhiteSpace(advisorNameRaw))
            { errors.Add($"الصف {rowIndex}: اسم المرشد غير موجود."); continue; }

            if (string.IsNullOrWhiteSpace(codeRaw))
            { errors.Add($"الصف {rowIndex}: كود الطالب غير موجود."); continue; }

            rows.Add((rowIndex, advisorNameRaw, codeRaw));
        }

        if (rows.Count == 0)
        {
            return Result.Success(new ImportExcelAssignmentsResponseDto(0, 0, 0, errors));
        }

        // 3. Batch-fetch only the relevant codes (not entire tables)
        var allCodes = rows.Select(r => r.Code).Distinct().ToList();

        var existingAssignments = await _context.AdvisorStudentAssignments
            .Where(a => allCodes.Contains(a.UniversityCode))
            .ToDictionaryAsync(a => a.UniversityCode, cancellationToken);

        var registeredStudents = await _context.Students
            .Where(s => allCodes.Contains(s.UniversityCode))
            .ToDictionaryAsync(s => s.UniversityCode, cancellationToken);

        // 4. Process rows
        var assignmentRepo = _unitOfWork.Repository<AdvisorStudentAssignment>();
        int totalProcessed = 0, successful = 0, skipped = 0;

        foreach (var (ri, advisorNameRaw, codeRaw) in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalProcessed++;
            var normName = NormalizeArabicName(advisorNameRaw);

            if (!advisorMap.TryGetValue(normName, out var advisorId))
            {
                errors.Add($"الصف {ri}: لم يتم العثور على المرشد الأكاديمي [{advisorNameRaw}] في النظام.");
                continue;
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

            // Update registered student if present
            if (registeredStudents.TryGetValue(codeRaw, out var student))
            {
                student.AcademicAdvisorId = advisorId;
            }
        }

        await _unitOfWork.CompleteAsync();

        return Result.Success(new ImportExcelAssignmentsResponseDto(
            totalProcessed, successful, skipped, errors));
    }

    // ═══════════════════════════════════════════════════════════════
    // Private Helpers — Reusable IQueryable Builders
    // ═══════════════════════════════════════════════════════════════

    private IQueryable<AssignedStudentProjection> BuildAssignedStudentQuery()
    {
        return from a in _context.AdvisorStudentAssignments.AsNoTracking()
               from s in _context.Students
                   .Where(s => s.UniversityCode == a.UniversityCode)
                   .DefaultIfEmpty()
               select new AssignedStudentProjection
               {
                   AdvisorId = a.AdvisorId,
                   AssignmentId = a.Id,
                   UniversityCode = a.UniversityCode,
                   StudentNameAr = s != null ? s.User.FirstNameAr + " " + s.User.LastNameAr : null,
                   StudentNameEn = s != null ? s.User.FirstNameEn + " " + s.User.LastNameEn : null,
                   IsStudentRegistered = s != null,
                   AssignedAt = a.AssignedAt
               };
    }

    private IQueryable<MyStudentProjection> BuildMyStudentQuery()
    {
        return from a in _context.AdvisorStudentAssignments.AsNoTracking()
               from s in _context.Students
                   .Where(s => s.UniversityCode == a.UniversityCode)
                   .DefaultIfEmpty()
               select new MyStudentProjection
               {
                   AdvisorId = a.AdvisorId,
                   AssignmentId = a.Id,
                   UniversityCode = a.UniversityCode,
                   IsRegistered = s != null,
                   AssignedAt = a.AssignedAt,
                   StudentId = s != null ? s.Id.ToString() : null,
                   FullNameAr = s != null ? s.User.FirstNameAr + " " + s.User.LastNameAr : null,
                   FullNameEn = s != null ? s.User.FirstNameEn + " " + s.User.LastNameEn : null,
                   NationalId = s != null ? s.NationalId : null,
                   Email = s != null ? s.User.Email : null,
                   PhoneNumber = s != null ? s.User.PhoneNumber : null,
                   AlternatePhone = s != null ? s.User.AlternatePhone : null,
                   Address = s != null ? s.Address : null
               };
    }

    private async Task<(List<T> Items, int TotalCount, int PageNum, int PageSize)> PaginateAsync<T>(
        IQueryable<T> query, int? pageNumber, int? pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var (pNum, pSize) = NormalizePagination(pageNumber, pageSize, totalCount);

        var items = await query
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, pNum, pSize);
    }

    private static (int PageNum, int PageSize) NormalizePagination(
        int? pageNumber, int? pageSize, int totalCount)
    {
        var pSize = pageSize is > 0
            ? Math.Min(pageSize.Value, MaxPageSize)
            : DefaultPageSize;

        var pNum = pageNumber is > 0 ? pageNumber.Value : 1;

        if (!pageNumber.HasValue && !pageSize.HasValue)
        {
            pSize = totalCount > 0 ? totalCount : 1;
        }

        return (pNum, pSize);
    }

    private static string NormalizeArabicName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        return name.Trim()
            .Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا')
            .Replace('ى', 'ي').Replace('ة', 'ه')
            .Replace("  ", " ");
    }

    // ═══════════════════════════════════════════════════════════════
    // Internal Projection Classes
    // ═══════════════════════════════════════════════════════════════

    private class AssignedStudentProjection
    {
        public string AdvisorId { get; init; } = default!;
        public int AssignmentId { get; init; }
        public string UniversityCode { get; init; } = default!;
        public string? StudentNameAr { get; init; }
        public string? StudentNameEn { get; init; }
        public bool IsStudentRegistered { get; init; }
        public DateTime AssignedAt { get; init; }

        public AssignedStudentDto ToDto() => new(
            AssignmentId, UniversityCode, StudentNameAr,
            StudentNameEn, IsStudentRegistered, AssignedAt);
    }

    private class MyStudentProjection
    {
        public string AdvisorId { get; init; } = default!;
        public int AssignmentId { get; init; }
        public string UniversityCode { get; init; } = default!;
        public bool IsRegistered { get; init; }
        public DateTime AssignedAt { get; init; }
        public string? StudentId { get; init; }
        public string? FullNameAr { get; init; }
        public string? FullNameEn { get; init; }
        public string? NationalId { get; init; }
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }
        public string? AlternatePhone { get; init; }
        public string? Address { get; init; }

        public AdvisorMyStudentItemDto ToDto() => new(
            AssignmentId, UniversityCode, IsRegistered, AssignedAt,
            StudentId, FullNameAr, FullNameEn, NationalId,
            Email, PhoneNumber, AlternatePhone, Address);
    }
}
