using Microsoft.AspNetCore.Mvc.Filters;

namespace URMS.Api.Middleware;

/// <summary>
/// CSRF protection for cookie-authenticated mutating requests.
/// Requires a custom "X-CSRF" header (value: "1") on all non-safe HTTP methods
/// when the request is authenticated via HttpOnly cookies (not Bearer token).
/// 
/// The frontend must include this header on all POST/PUT/PATCH/DELETE requests:
///     headers: { "X-CSRF": "1" }
/// 
/// Why this works:
/// Browsers will never send custom headers in cross-origin requests without
/// explicit CORS permission. Since our CORS is locked to specific origins,
/// a CSRF attacker's page cannot add the X-CSRF header.
/// </summary>
public class CsrfHeaderFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpMethod = context.HttpContext.Request.Method;

        // Safe (read-only) HTTP methods are not vulnerable to CSRF
        if (SafeMethods.Contains(httpMethod))
        {
            await next();
            return;
        }

        // [AllowAnonymous] endpoints don't rely on cookie sessions — no CSRF risk
        var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is IAllowAnonymous);
        if (hasAllowAnonymous)
        {
            await next();
            return;
        }

        // If request carries a Bearer Authorization header, it's not cookie-auth — skip
        var hasAuthorizationHeader = context.HttpContext.Request.Headers
            .ContainsKey("Authorization");
        if (hasAuthorizationHeader)
        {
            await next();
            return;
        }

        // Cookie-authenticated mutating request — require X-CSRF header
        var hasAccessTokenCookie = context.HttpContext.Request.Cookies
            .ContainsKey(AuthConstants.AccessTokenCookie);

        if (hasAccessTokenCookie && !context.HttpContext.Request.Headers.ContainsKey("X-CSRF"))
        {
            var localizer = context.HttpContext.RequestServices.GetService<ILocalizationService>();
            var msg = localizer?.GetLocalizedString("CsrfValidationFailed")
                ?? "Missing CSRF protection header. Include 'X-CSRF: 1' in your request headers.";

            var response = ApiResponse.Failure(
                message: msg,
                errors: [new ApiError("CSRF.HeaderMissing", msg)],
                statusCode: StatusCodes.Status403Forbidden
            );

            context.Result = new ObjectResult(response) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}
