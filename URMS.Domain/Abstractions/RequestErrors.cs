namespace URMS.Domain.Abstractions;

public static class RequestErrors
{
    public static readonly Error RequestNotFound =
        new("Request.NotFound", "Request not found.", 404);

    public static readonly Error InvalidStatusForAdvisorReview =
        new("Request.InvalidStatusForReview", "Cannot review request in its current status.", 409);

    public static readonly Error InvalidStatusForStaffConfirm =
        new("Request.InvalidStatusForStaff", "Cannot process staff confirmation for request in its current status.", 409);

    public static readonly Error StudentNotFound =
        new("Student.NotFound", "Student not found.", 404);

    public static readonly Error StudentNotApproved =
        new("Student.NotApproved", "Your student account is pending approval by advisor/secretary.", 403);

    public static readonly Error GpaTooLow =
        new("Request.GpaTooLow", "Extra hours registration requires a minimum GPA of 3.00.", 400);
}

public static class FormErrors
{
    public static readonly Error FormNotFound =
        new("Form.NotFound", "Form definition not found.", 404);

    public static readonly Error FormClosed =
        new("Form.Closed", "Form submission is currently closed by administration.", 400);

    public static Error RequiredFieldMissing(string labelAr) =>
        new("Form.RequiredFieldMissing", $"الحقل الإجباري [{labelAr}] غير موجود أو فارغ.", 400);

    public static Error InvalidFieldType(string labelAr) =>
        new("Form.InvalidFieldType", $"القيمة المرفقة للحقل [{labelAr}] غير صالحة.", 400);
}

public static class UserErrors
{
    public static readonly Error UserNotFound =
        new("User.NotFound", "User not found.", 404);

    public static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid email or password.", 401);

    public static readonly Error AccountDeactivated =
        new("Auth.AccountDeactivated", "Account is deactivated.", 403);

    public static readonly Error AccountNotApproved =
        new("Auth.AccountNotApproved", "Your account is pending approval by your Academic Advisor.", 403);

    public static readonly Error DuplicateEmail =
        new("Auth.DuplicateEmail", "User with this email already exists.", 409);

    public static readonly Error StudentAlreadyApproved =
        new("User.AlreadyApproved", "Student is already approved.", 409);

    public static readonly Error InvalidRefreshToken =
        new("Auth.InvalidRefreshToken", "Invalid refresh token.", 401);

    public static readonly Error RefreshTokenInactive =
        new("Auth.RefreshTokenInactive", "Refresh token is inactive or expired.", 401);

    public static Error RegistrationFailed(string details) =>
        new("Auth.RegistrationFailed", $"Student registration failed: {details}", 400);

    public static Error ChangePasswordFailed(string details) =>
        new("Auth.ChangePasswordFailed", $"Change password failed: {details}", 400);
}
