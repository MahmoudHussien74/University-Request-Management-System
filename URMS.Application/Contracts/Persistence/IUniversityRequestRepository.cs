using System.Linq.Expressions;
using URMS.Domain.Contracts;

namespace URMS.Application.Contracts.Persistence;

/// <summary>
/// Domain-specific repository for UniversityRequest.
/// Encapsulates complex EF Core queries (Includes, Search, Pagination)
/// so the Application layer never touches IQueryable or EF Core directly.
/// </summary>
public interface IUniversityRequestRepository : IGenericRepository<UniversityRequest>
{
    /// <summary>
    /// Retrieves paginated requests with full navigation includes, status filter, and search.
    /// </summary>
    Task<(List<UniversityRequest> Items, int TotalCount)> GetRequestsPagedAsync(
        Expression<Func<UniversityRequest, bool>>? ownershipFilter = null,
        RequestStatus? status = null,
        string? searchColumn = null,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null);

    /// <summary>
    /// Retrieves a single request by ID with full navigation includes (Student, Advisor, Administration, HistoryLogs).
    /// </summary>
    Task<UniversityRequest?> GetByIdWithDetailsAsync(int requestId);

    /// <summary>
    /// Retrieves a single request by confirmation token with full navigation includes.
    /// </summary>
    Task<UniversityRequest?> GetByTokenWithDetailsAsync(string token);

    /// <summary>
    /// Retrieves a single request by ID with includes needed for workflow operations (write path).
    /// </summary>
    Task<UniversityRequest?> GetForWorkflowAsync(int requestId);

    /// <summary>
    /// Retrieves a single request by ID with includes needed for send-to-administration (includes FormDefinition.Fields).
    /// </summary>
    Task<UniversityRequest?> GetForAdministrationSendAsync(int requestId);

    /// <summary>
    /// Retrieves a single request by confirmation token with includes needed for external response workflow.
    /// </summary>
    Task<UniversityRequest?> GetByTokenForWorkflowAsync(string token);

    /// <summary>
    /// Retrieves an ApplicationUser by ID with their Student profile included.
    /// Used during request creation to validate and auto-assign the academic advisor.
    /// </summary>
    Task<ApplicationUser?> GetStudentWithProfileAsync(string userId);

    /// <summary>
    /// Retrieves an ApplicationUser by ID (lightweight, no includes).
    /// </summary>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
}
