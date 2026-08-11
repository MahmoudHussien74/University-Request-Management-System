using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using URMS.Application.Common.Models;
using URMS.Application.Contracts.Infrastructure;

namespace URMS.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled Exception: {Message}", exception.Message);

        var localizer = httpContext.RequestServices.GetService<ILocalizationService>();
        var msg = localizer?.GetLocalizedString("InternalServerError");
        if (string.IsNullOrWhiteSpace(msg) || msg == "InternalServerError")
        {
            msg = "An unexpected error occurred on the server.";
        }

        var failureResponse = ApiResponse.Failure(
            message: msg,
            errors: [new ApiError("InternalServerError", exception.Message)],
            statusCode: StatusCodes.Status500InternalServerError
        );

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(failureResponse, cancellationToken);

        return true;
    }
}
