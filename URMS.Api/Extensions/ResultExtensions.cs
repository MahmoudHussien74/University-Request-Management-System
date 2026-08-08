using Microsoft.AspNetCore.Mvc;
using URMS.Domain.Abstractions;

namespace URMS.Api.Extensions;

public static class ResultExtensions
{
    public static ObjectResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");

        var problemDetails = new ProblemDetails
        {
            Status = result.Error.StatusCode,
            Title = result.Error.Code,
            Detail = result.Error.Message,
            Extensions =
            {
                ["errors"] = new[]
                {
                    new
                    {
                        result.Error.Code,
                        result.Error.Message
                    }
                }
            }
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = result.Error.StatusCode
        };
    }
}
