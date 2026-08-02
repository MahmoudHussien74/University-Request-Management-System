using URMS.Domain.Common;

namespace URMS.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresOn { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedOn { get; set; }

    // ─── Navigation ───
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
    public bool IsActive => !IsRevoked && !IsExpired;
}
