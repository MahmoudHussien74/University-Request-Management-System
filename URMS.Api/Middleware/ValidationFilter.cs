using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace URMS.Api.Middleware;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator is not null)
            {
                var validationContext = new ValidationContext<object>(argument);
                var validationResult = await validator.ValidateAsync(validationContext);

                if (!validationResult.IsValid)
                {
                    var localizer = context.HttpContext.RequestServices.GetService<ILocalizationService>();

                    var errors = validationResult.Errors
                        .Select(e => new ApiError(e.PropertyName, localizer?.GetLocalizedString(e.ErrorMessage) ?? e.ErrorMessage))
                        .ToList();

                    var msg = localizer?.GetLocalizedString("ValidationFailed");
                    if (string.IsNullOrWhiteSpace(msg) || msg == "ValidationFailed")
                    {
                        msg = "Validation failed for one or more fields.";
                    }

                    var failureResponse = ApiResponse.Failure(
                        message: msg,
                        errors: errors,
                        statusCode: StatusCodes.Status400BadRequest
                    );

                    context.Result = new BadRequestObjectResult(failureResponse);
                    return;
                }
            }
        }

        await next();
    }
}
