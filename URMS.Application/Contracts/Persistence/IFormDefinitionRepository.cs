using URMS.Application.DTOs.Forms;
using URMS.Domain.Contracts;

namespace URMS.Application.Contracts.Persistence;

/// <summary>
/// Domain-specific repository for FormDefinition.
/// Encapsulates all EF Core query logic (Includes, projections, date-based filtering)
/// so the Application layer stays free of infrastructure concerns.
/// </summary>
public interface IFormDefinitionRepository : IGenericRepository<FormDefinition>
{
    /// <summary>
    /// Retrieves a non-deleted form by ID with Fields and Requests included.
    /// Used for update, toggle status, and get-by-id operations.
    /// </summary>
    Task<FormDefinition?> GetByIdWithDetailsAsync(int id);

    /// <summary>
    /// Retrieves a non-deleted form by ID with Fields included (no Requests).
    /// Used for field management and submission validation.
    /// </summary>
    Task<FormDefinition?> GetByIdWithFieldsAsync(int id);

    /// <summary>
    /// Retrieves all non-deleted forms with Fields and Requests, ordered by creation date descending.
    /// </summary>
    Task<List<FormDefinition>> GetAllForAdminAsync();

    /// <summary>
    /// Retrieves active, non-deleted forms within the valid date range, with Fields and Requests.
    /// </summary>
    Task<List<FormDefinition>> GetActiveForStudentsAsync(DateTime now);

    /// <summary>
    /// Retrieves active, non-deleted forms within date range, projected to lightweight summary DTOs.
    /// </summary>
    Task<List<FormSummaryDto>> GetActiveSummariesAsync(DateTime now);
}
