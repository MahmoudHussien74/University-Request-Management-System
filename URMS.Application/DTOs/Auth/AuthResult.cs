namespace URMS.Application.DTOs.Auth;

/// <summary>
/// Internal result from AuthService — never sent to client.
/// Carries tokens for HttpOnly cookie setting + user info for the response body.
/// </summary>
public record AuthResult(
    AuthResponseDto User,
    string AccessToken,
    DateTime AccessTokenExpiresOn,
    string RefreshToken,
    DateTime RefreshTokenExpiresOn
);
