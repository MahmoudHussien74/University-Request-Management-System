using URMS.Domain.Entities;

namespace URMS.Application.Contracts.Identity;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresOn) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
    RefreshToken GenerateRefreshToken();
}
