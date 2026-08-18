namespace URMS.Domain.Constants;

public static class LocalizationKeys
{
    // General / System Keys
    public const string SuccessDefault = "SuccessDefault";
    public const string InternalServerError = "InternalServerError";
    public const string ValidationFailed = "ValidationFailed";
    public const string UnauthorizedAccess = "UnauthorizedAccess";
    public const string ForbiddenAccess = "ForbiddenAccess";
    public const string RateLimitExceeded = "RateLimitExceeded";
    public const string CsrfValidationFailed = "CsrfValidationFailed";

    // Authentication Keys
    public const string LoginSuccessful = "LoginSuccessful";
    public const string UserRegisteredSuccessfully = "UserRegisteredSuccessfully";
    public const string LogoutSuccessful = "LogoutSuccessful";
    public const string PasswordChangedSuccessfully = "PasswordChangedSuccessfully";

    // Request Lifecycle Keys
    public const string AdvisorReviewSuccess = "AdvisorReviewSuccess";
    public const string SentToAdministrationSuccess = "SentToAdministrationSuccess";
    public const string ExternalResponseSuccess = "ExternalResponseSuccess";
    public const string RequestWithdrawnSuccess = "RequestWithdrawnSuccess";
    public const string StatusOverrideSuccess = "StatusOverrideSuccess";
}
