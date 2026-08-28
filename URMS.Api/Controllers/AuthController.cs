using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;

namespace URMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Student registration — requires approval before activation.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequest request)
    {
        var result = await _authService.RegisterStudentAsync(request);

        return result.ToResponse(HttpContext, LocalizationKeys.UserRegisteredSuccessfully);
    }

    /// <summary>
    /// User Login — sets HttpOnly cookies for JWT Access Token + Refresh Token.
    /// Response body contains user info only (no tokens).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result.IsFailure)
            return result.ToResponse(HttpContext);

        var authResult = result.Value;
        SetAuthCookies(authResult.AccessToken, authResult.RefreshToken, authResult.RefreshTokenExpiresOn);

        return Result.Success(authResult.User).ToResponse(HttpContext, LocalizationKeys.LoginSuccessful);
    }

    /// <summary>
    /// Refresh JWT Access Token using Refresh Token from HttpOnly cookie.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken()
    {
        var accessToken = Request.Cookies[AuthConstants.AccessTokenCookie] ?? string.Empty;
        var refreshToken = Request.Cookies[AuthConstants.RefreshTokenCookie];

        if (string.IsNullOrEmpty(refreshToken))
            return Result.Failure(UserErrors.InvalidRefreshToken).ToResponse(HttpContext);

        var result = await _authService.RefreshTokenAsync(accessToken, refreshToken);

        if (result.IsFailure)
            return result.ToResponse(HttpContext);

        var authResult = result.Value;
        SetAuthCookies(authResult.AccessToken, authResult.RefreshToken, authResult.RefreshTokenExpiresOn);

        return Result.Success(authResult.User).ToResponse(HttpContext, LocalizationKeys.LoginSuccessful);
    }

    /// <summary>
    /// Revoke the refresh token (from HttpOnly cookie) and clear auth cookies.
    /// </summary>
    [HttpPost("revoke-refresh-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeToken()
    {
        var refreshToken = Request.Cookies[AuthConstants.RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Result.Failure(UserErrors.InvalidRefreshToken).ToResponse(HttpContext);

        var result = await _authService.RevokeTokenAsync(refreshToken);

        if (result.IsFailure)
            return result.ToResponse(HttpContext);

        ClearAuthCookies();

        return result.ToResponse(HttpContext, LocalizationKeys.SuccessDefault);
    }

    /// <summary>
    /// Change password for logged-in user.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.ChangePasswordAsync(userId, request);

        return result.ToResponse(HttpContext, LocalizationKeys.PasswordChangedSuccessfully);
    }

    /// <summary>
    /// Get currently authenticated user details with roles and permissions.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.GetCurrentUserAsync(userId);

        return result.ToResponse(HttpContext);
    }

    private void SetAuthCookies(string accessToken, string refreshToken, DateTime refreshTokenExpiresOn)
    {
        var isHttps = Request.IsHttps;
        var sameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax;

        // Access token: session cookie (no Expires — governed by JWT exp claim)
        Response.Cookies.Append(AuthConstants.AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = sameSite,
            Secure = isHttps,
            Path = "/"
        });

        // Refresh token: persistent cookie with explicit expiration
        Response.Cookies.Append(AuthConstants.RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = sameSite,
            Secure = isHttps,
            Path = "/",
            Expires = refreshTokenExpiresOn
        });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AuthConstants.AccessTokenCookie);
        Response.Cookies.Delete(AuthConstants.RefreshTokenCookie);
    }
}
