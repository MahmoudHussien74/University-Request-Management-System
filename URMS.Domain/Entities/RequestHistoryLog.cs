using URMS.Domain.Common;
using URMS.Domain.Enums;

namespace URMS.Domain.Entities;

public class RequestHistoryLog : AuditableEntity
{
    public int UniversityRequestId { get; set; }
    public UniversityRequest UniversityRequest { get; set; } = null!;

    public string ActionById { get; set; } = null!;
    public ApplicationUser ActionBy { get; set; } = null!;

    public RequestStatus OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; }

    public string ActionMessage { get; set; } = null!;
    public string? Notes { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}
