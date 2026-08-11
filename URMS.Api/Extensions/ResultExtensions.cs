using Microsoft.AspNetCore.Mvc;
using URMS.Application.Common.Models;
using URMS.Application.Contracts.Infrastructure;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;

namespace URMS.Api.Extensions;

public static class ResultExtensions
{
    public static ObjectResult ToResponse<T>(this Result<T> result, HttpContext? httpContext = null, string successMessage = LocalizationKeys.SuccessDefault, int successStatusCode = 200)
    {
        var localizer = httpContext?.RequestServices.GetService<ILocalizationService>();

        if (result.IsSuccess)
        {
            var msg = GetText(localizer, successMessage, "Operation completed successfully.");
            var response = ApiResponse<T>.Success(result.Value, msg, successStatusCode);
            return new ObjectResult(response) { StatusCode = successStatusCode };
        }

        var error = result.Error;
        var statusCode = error.StatusCode ?? 400;
        var translatedMessage = GetText(localizer, error.Code, error.Message);

        var failureResponse = ApiResponse<T>.Failure(
            message: translatedMessage,
            errors: [new ApiError(error.Code, translatedMessage)],
            statusCode: statusCode
        );

        return new ObjectResult(failureResponse) { StatusCode = statusCode };
    }

    public static ObjectResult ToResponse(this Result result, HttpContext? httpContext = null, string successMessage = LocalizationKeys.SuccessDefault, int successStatusCode = 200)
    {
        var localizer = httpContext?.RequestServices.GetService<ILocalizationService>();

        if (result.IsSuccess)
        {
            var msg = GetText(localizer, successMessage, "Operation completed successfully.");
            var response = ApiResponse.Success(msg, successStatusCode);
            return new ObjectResult(response) { StatusCode = successStatusCode };
        }

        var error = result.Error;
        var statusCode = error.StatusCode ?? 400;
        var translatedMessage = GetText(localizer, error.Code, error.Message);

        var failureResponse = ApiResponse.Failure(
            message: translatedMessage,
            errors: [new ApiError(error.Code, translatedMessage)],
            statusCode: statusCode
        );

        return new ObjectResult(failureResponse) { StatusCode = statusCode };
    }

    public static ObjectResult ToProblem(this Result result, HttpContext? httpContext = null)
    {
        return result.ToResponse(httpContext);
    }

    private static string GetText(ILocalizationService? localizer, string key, string? fallback = null)
    {
        if (localizer == null) return fallback ?? key;

        var text = localizer.GetLocalizedString(key);
        if (text != key && !string.IsNullOrWhiteSpace(text)) return text;

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            var fallbackText = localizer.GetLocalizedString(fallback);
            if (fallbackText != fallback && !string.IsNullOrWhiteSpace(fallbackText)) return fallbackText;
            return fallback;
        }

        return key;
    }
}
