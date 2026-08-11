using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Api.Extensions;
using URMS.Application.Contracts.Identity;
using URMS.Application.DTOs.Auth;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;

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
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequest request)
    {
        var result = await _authService.RegisterStudentAsync(request);

        return result.ToResponse(HttpContext, LocalizationKeys.UserRegisteredSuccessfully);
    }

    /// <summary>
    /// User Login — generates JWT Access Token + Refresh Token and sets HttpOnly Cookies.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result.IsFailure)
            return result.ToResponse(HttpContext);

        SetAuthCookies(result.Value.Token, result.Value.RefreshToken, result.Value.RefreshTokenExpiresOn);
        return result.ToResponse(HttpContext, LocalizationKeys.LoginSuccessful);
    }

    /// <summary>
    /// Refresh JWT Access Token using Refresh Token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        var token = request?.Token ?? Request.Cookies[AuthConstants.AccessTokenCookie];
        var refreshToken = request?.RefreshToken ?? Request.Cookies[AuthConstants.RefreshTokenCookie];

        if (string.IsNullOrEmpty(refreshToken))
            return Result.Failure(UserErrors.InvalidRefreshToken).ToResponse(HttpContext);

        var result = await _authService.RefreshTokenAsync(token ?? string.Empty, refreshToken);

        if (result.IsFailure)
            return result.ToResponse(HttpContext);

        SetAuthCookies(result.Value.Token, result.Value.RefreshToken, result.Value.RefreshTokenExpiresOn);
        return result.ToResponse(HttpContext, LocalizationKeys.LoginSuccessful);
    }

    /// <summary>
    /// Revoke a refresh token.
    /// </summary>
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest? request)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies[AuthConstants.RefreshTokenCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Result.Failure(UserErrors.InvalidRefreshToken).ToResponse(HttpContext);

        var result = await _authService.RevokeTokenAsync(refreshToken);

        if (result.IsFailure)
            return result.ToResponse(HttpContext);

        Response.Cookies.Delete(AuthConstants.AccessTokenCookie);
        Response.Cookies.Delete(AuthConstants.RefreshTokenCookie);

        return result.ToResponse(HttpContext, LocalizationKeys.SuccessDefault);
    }

    /// <summary>
    /// User Logout — revokes refresh token and clears auth cookies.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[AuthConstants.RefreshTokenCookie];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeTokenAsync(refreshToken);
        }

        await _authService.LogoutAsync();

        Response.Cookies.Delete(AuthConstants.AccessTokenCookie);
        Response.Cookies.Delete(AuthConstants.RefreshTokenCookie);

        return Result.Success().ToResponse(HttpContext, LocalizationKeys.LogoutSuccessful);
    }

    /// <summary>
    /// Change password for logged-in user.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.ChangePasswordAsync(userId, request);

        return result.ToResponse(HttpContext, LocalizationKeys.PasswordChangedSuccessfully);
    }

    /// <summary>
    /// Get currently authenticated user details with roles & permissions.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
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
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Secure = isHttps
        };

        Response.Cookies.Append(AuthConstants.AccessTokenCookie, accessToken, cookieOptions);
        Response.Cookies.Append(AuthConstants.RefreshTokenCookie, refreshToken, cookieOptions.WithExpires(refreshTokenExpiresOn));
    }
}

public static class CookieOptionsExtensions
{
    public static CookieOptions WithExpires(this CookieOptions options, DateTime expires)
    {
        options.Expires = expires;
        return options;
    }
}
