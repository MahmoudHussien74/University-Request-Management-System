using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using URMS.Application.Contracts.Persistence;
using URMS.Domain.Entities;
using URMS.Domain.Enums;

namespace URMS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Infrastructure implementation of IUniversityRequestRepository.
/// All EF Core-specific Include/Search/Pagination logic is encapsulated here.
/// </summary>
public class UniversityRequestRepository : GenericRepository<UniversityRequest>, IUniversityRequestRepository
{
    public UniversityRequestRepository(AppDbContext context) : base(context) { }

    public async Task<(List<UniversityRequest> Items, int TotalCount)> GetRequestsPagedAsync(
        Expression<Func<UniversityRequest, bool>>? ownershipFilter = null,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        IQueryable<UniversityRequest> query = GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy);

        if (ownershipFilter != null)
            query = query.Where(ownershipFilter);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = ApplySearch(query, searchColumn, searchTerm);

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        List<UniversityRequest> items;
        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            items = await query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
        }
        else
        {
            items = await query.ToListAsync();
        }

        return (items, totalCount);
    }

    public async Task<UniversityRequest?> GetByIdWithDetailsAsync(int requestId)
    {
        return await GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<UniversityRequest?> GetByTokenWithDetailsAsync(string token)
    {
        return await GetQueryable()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.ConfirmationToken == token);
    }

    public async Task<UniversityRequest?> GetForWorkflowAsync(int requestId)
    {
        return await GetQueryable()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.Advisor)
            .Include(r => r.Administration)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<UniversityRequest?> GetForAdministrationSendAsync(int requestId)
    {
        return await GetQueryable()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition!).ThenInclude(f => f.Fields)
            .Include(r => r.Advisor)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<UniversityRequest?> GetByTokenForWorkflowAsync(string token)
    {
        return await GetQueryable()
            .AsSplitQuery()
            .Include(r => r.Student).ThenInclude(u => u.Student)
            .Include(r => r.FormDefinition)
            .Include(r => r.Advisor)
            .Include(r => r.HistoryLogs).ThenInclude(l => l.ActionBy)
            .FirstOrDefaultAsync(r => r.ConfirmationToken == token);
    }

    public async Task<ApplicationUser?> GetStudentWithProfileAsync(string userId)
    {
        return await _context.Set<ApplicationUser>()
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    // ─── Private: Search Logic Encapsulated in Infrastructure ───
    private static IQueryable<UniversityRequest> ApplySearch(IQueryable<UniversityRequest> query, string? searchColumn, string searchTerm)
    {
        var term = $"%{searchTerm.Trim()}%";
        var col = searchColumn?.Trim().ToLower();

        return col switch
        {
            "title" => query.Where(r => r.FormDefinition != null &&
                (EF.Functions.Like(r.FormDefinition.TitleAr, term) || EF.Functions.Like(r.FormDefinition.TitleEn, term))),

            "code" or "universitycode" => query.Where(r => r.Student != null && r.Student.Student != null &&
                EF.Functions.Like(r.Student.Student.UniversityCode, term)),

            "studentname" or "name" => query.Where(r => r.Student != null && (
                EF.Functions.Like(r.Student.FirstNameAr, term) || EF.Functions.Like(r.Student.LastNameAr, term) ||
                (r.Student.SecondNameAr != null && EF.Functions.Like(r.Student.SecondNameAr, term)) ||
                (r.Student.ThirdNameAr != null && EF.Functions.Like(r.Student.ThirdNameAr, term)) ||
                EF.Functions.Like(r.Student.FirstNameEn, term) || EF.Functions.Like(r.Student.LastNameEn, term) ||
                (r.Student.SecondNameEn != null && EF.Functions.Like(r.Student.SecondNameEn, term)) ||
                (r.Student.ThirdNameEn != null && EF.Functions.Like(r.Student.ThirdNameEn, term))
            )),

            "advisorname" => query.Where(r => r.Advisor != null && (
                EF.Functions.Like(r.Advisor.FirstNameAr, term) || EF.Functions.Like(r.Advisor.LastNameAr, term) ||
                (r.Advisor.SecondNameAr != null && EF.Functions.Like(r.Advisor.SecondNameAr, term)) ||
                (r.Advisor.ThirdNameAr != null && EF.Functions.Like(r.Advisor.ThirdNameAr, term)) ||
                EF.Functions.Like(r.Advisor.FirstNameEn, term) || EF.Functions.Like(r.Advisor.LastNameEn, term) ||
                (r.Advisor.SecondNameEn != null && EF.Functions.Like(r.Advisor.SecondNameEn, term)) ||
                (r.Advisor.ThirdNameEn != null && EF.Functions.Like(r.Advisor.ThirdNameEn, term))
            )),

            "reason" or "rejectionreason" => query.Where(r => r.RejectionReason != null &&
                EF.Functions.Like(r.RejectionReason, term)),

            // Default: search across all fields
            _ => query.Where(r =>
                (r.Student != null && (
                    EF.Functions.Like(r.Student.FirstNameAr, term) || EF.Functions.Like(r.Student.LastNameAr, term) ||
                    (r.Student.SecondNameAr != null && EF.Functions.Like(r.Student.SecondNameAr, term)) ||
                    (r.Student.ThirdNameAr != null && EF.Functions.Like(r.Student.ThirdNameAr, term)) ||
                    EF.Functions.Like(r.Student.FirstNameEn, term) || EF.Functions.Like(r.Student.LastNameEn, term) ||
                    (r.Student.SecondNameEn != null && EF.Functions.Like(r.Student.SecondNameEn, term)) ||
                    (r.Student.ThirdNameEn != null && EF.Functions.Like(r.Student.ThirdNameEn, term)) ||
                    (r.Student.Student != null && EF.Functions.Like(r.Student.Student.UniversityCode, term))
                )) ||
                (r.FormDefinition != null && (EF.Functions.Like(r.FormDefinition.TitleAr, term) || EF.Functions.Like(r.FormDefinition.TitleEn, term))) ||
                (r.Advisor != null && (
                    EF.Functions.Like(r.Advisor.FirstNameAr, term) || EF.Functions.Like(r.Advisor.LastNameAr, term) ||
                    (r.Advisor.SecondNameAr != null && EF.Functions.Like(r.Advisor.SecondNameAr, term)) ||
                    (r.Advisor.ThirdNameAr != null && EF.Functions.Like(r.Advisor.ThirdNameAr, term)) ||
                    EF.Functions.Like(r.Advisor.FirstNameEn, term) || EF.Functions.Like(r.Advisor.LastNameEn, term) ||
                    (r.Advisor.SecondNameEn != null && EF.Functions.Like(r.Advisor.SecondNameEn, term)) ||
                    (r.Advisor.ThirdNameEn != null && EF.Functions.Like(r.Advisor.ThirdNameEn, term))
                ))
            )
        };
    }
}
