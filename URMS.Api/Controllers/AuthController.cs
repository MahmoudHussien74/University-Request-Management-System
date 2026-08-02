using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<UserResponse>> RegisterStudent([FromBody] RegisterStudentRequest request)
    {
        var response = await _authService.RegisterStudentAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// User Login — generates JWT Access Token + Refresh Token and sets HttpOnly Cookies.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        SetAuthCookies(response.Token, response.RefreshToken, response.RefreshTokenExpiresOn);
        return Ok(response);
    }

    /// <summary>
    /// Refresh JWT Access Token using Refresh Token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        var token = request?.Token ?? Request.Cookies["accessToken"];
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("Refresh token is required.");

        var response = await _authService.RefreshTokenAsync(token ?? string.Empty, refreshToken);
        SetAuthCookies(response.Token, response.RefreshToken, response.RefreshTokenExpiresOn);
        return Ok(response);
    }

    /// <summary>
    /// Revoke a refresh token.
    /// </summary>
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest? request)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("Refresh token is required.");

        var result = await _authService.RevokeTokenAsync(refreshToken);
        if (!result)
            return BadRequest("Invalid or already revoked token.");

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Token revoked successfully." });
    }

    /// <summary>
    /// User Logout — revokes refresh token and clears auth cookies.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeTokenAsync(refreshToken);
        }

        await _authService.LogoutAsync();

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Logged out successfully." });
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

        await _authService.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>
    /// Get currently authenticated user details with roles & permissions.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user is null)
            return NotFound();

        return Ok(user);
    }

    private void SetAuthCookies(string accessToken, string refreshToken, DateTime refreshTokenExpiresOn)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false // set true in production HTTPS
        };

        Response.Cookies.Append("accessToken", accessToken, cookieOptions);
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions.WithExpires(refreshTokenExpiresOn));
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
