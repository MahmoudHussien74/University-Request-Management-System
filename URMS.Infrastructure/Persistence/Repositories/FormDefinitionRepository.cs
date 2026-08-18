using URMS.Application.DTOs.Forms;

namespace URMS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Infrastructure implementation of IFormDefinitionRepository.
/// All EF Core Include/filter/projection logic lives here.
/// </summary>
public class FormDefinitionRepository : GenericRepository<FormDefinition>, IFormDefinitionRepository
{
    public FormDefinitionRepository(AppDbContext context) : base(context) { }

    public async Task<FormDefinition?> GetByIdWithDetailsAsync(int id)
    {
        return await GetQueryable()
            .Include(f => f.Fields.OrderBy(field => field.Order))
            .Include(f => f.Requests)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
    }

    public async Task<FormDefinition?> GetByIdWithFieldsAsync(int id)
    {
        return await GetQueryable()
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
    }

    public async Task<List<FormDefinition>> GetAllForAdminAsync()
    {
        return await GetQueryable()
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(field => field.Order))
            .Include(f => f.Requests)
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FormDefinition>> GetActiveForStudentsAsync(DateTime now)
    {
        return await GetQueryable()
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(field => field.Order))
            .Include(f => f.Requests)
            .Where(f => f.IsActive && !f.IsDeleted &&
                 (!f.StartDate.HasValue || f.StartDate.Value <= now) &&
                 (!f.EndDate.HasValue || f.EndDate.Value >= now))
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FormSummaryDto>> GetActiveSummariesAsync(DateTime now)
    {
        return await GetQueryable()
            .AsNoTracking()
            .Where(f => !f.IsDeleted &&
                        f.IsActive &&
                        (!f.StartDate.HasValue || f.StartDate.Value <= now) &&
                        (!f.EndDate.HasValue || f.EndDate.Value >= now))
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FormSummaryDto(
                f.Id,
                f.TitleAr,
                f.TitleEn,
                f.Description,
                f.StartDate,
                f.EndDate,
                f.Requests.Count))
            .ToListAsync();
    }
}
