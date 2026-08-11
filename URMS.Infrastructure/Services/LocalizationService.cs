using System.Globalization;
using Microsoft.Extensions.Localization;
using URMS.Application.Contracts.Infrastructure;
using URMS.Infrastructure.Resources;

namespace URMS.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    private static readonly Dictionary<string, (string Ar, string En)> _fallbackTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        // System & Pipeline
        ["SuccessDefault"] = ("تمت العملية بنجاح.", "Operation completed successfully."),
        ["ValidationFailed"] = ("يرجى مراجعة البيانات المدخلة والتأكد من صحتها.", "Please review the entered information and correct any errors."),
        ["InternalServerError"] = ("حدث خطأ فني غير متوقع. يرجى المحاولة لاحقاً.", "An unexpected technical error occurred. Please try again later."),
        ["UnauthorizedAccess"] = ("عفواً، يجب تسجيل الدخول للوصول إلى هذه الصفحة.", "Unauthorized access. Please log in to continue."),
        ["ForbiddenAccess"] = ("عفواً، ليس لديك صلاحية لتنفيذ هذا الإجراء.", "You do not have permission to perform this action."),

        // Auth & Account Success
        ["UserRegisteredSuccessfully"] = ("تم إنشاء حساب الطالب بنجاح، وفي انتظار موافقة المرشد الأكاديمي.", "Student account registered successfully. Pending academic advisor approval."),
        ["LoginSuccessful"] = ("تم تسجيل الدخول بنجاح.", "Logged in successfully."),
        ["LogoutSuccessful"] = ("تم تسجيل الخروج بنجاح.", "Logged out successfully."),
        ["PasswordChangedSuccessfully"] = ("تم تغيير كلمة المرور بنجاح.", "Password changed successfully."),
        ["StudentApprovedSuccessfully"] = ("تمت الموافقة على حساب الطالب وتفعيله بنجاح.", "Student account approved and activated successfully."),
        ["AccountDeactivatedSuccessfully"] = ("تم تعطيل الحساب بنجاح.", "Account deactivated successfully."),
        ["AccountReactivatedSuccessfully"] = ("تم إعادة تفعيل الحساب بنجاح.", "Account reactivated successfully."),
        ["AdvisorCreatedSuccessfully"] = ("تم إنشاء حساب المرشد الأكاديمي بنجاح.", "Academic advisor account created successfully."),
        ["BulkAdvisorsCreatedSuccessfully"] = ("تمت إضافة حسابات المرشدين الأكاديميين بنجاح.", "Academic advisor accounts created successfully."),
        ["StudentsAssignedSuccessfully"] = ("تم توزيع الطلاب على المرشد الأكاديمي بنجاح.", "Students assigned to advisor successfully."),
        ["AssignmentRemovedSuccessfully"] = ("تم إزالة توزيع الطالب بنجاح.", "Student assignment removed successfully."),

        // Request Operation Success
        ["AdvisorReviewSuccess"] = ("تم تسجيل مراجعة المرشد الأكاديمي على الطلب بنجاح.", "Advisor review recorded successfully."),
        ["SentToAdministrationSuccess"] = ("تم إرسال الطلب إلى إدارة الكلية بنجاح.", "Request sent to administration successfully."),
        ["ExternalResponseSuccess"] = ("تم تسجيل رد الإدارة على الطلب بنجاح.", "Administration response recorded successfully."),
        ["RequestWithdrawnSuccess"] = ("تم سحب الطلب بنجاح.", "Request withdrawn successfully."),
        ["StatusOverrideSuccess"] = ("تم تحديث حالة الطلب بنجاح.", "Request status updated successfully."),

        // User & Auth Errors
        ["User.NotFound"] = ("عفواً، لم يتم العثور على حساب المستخدم.", "User account could not be found."),
        ["UserNotFound"] = ("عفواً، لم يتم العثور على حساب المستخدم.", "User account could not be found."),
        ["UserNotFoundMessage"] = ("عفواً، لم يتم العثور على حساب المستخدم.", "User account could not be found."),
        ["Auth.InvalidCredentials"] = ("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "Incorrect email address or password."),
        ["User.InvalidCredentials"] = ("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "Incorrect email address or password."),
        ["InvalidCredentials"] = ("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "Incorrect email address or password."),
        ["InvalidCredentialsMessage"] = ("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "Incorrect email address or password."),
        ["Auth.AccountDeactivated"] = ("هذا الحساب معطل حالياً. يرجى التواصل مع إدارة الكلية.", "This account is currently deactivated. Please contact administration."),
        ["User.AccountDeactivated"] = ("هذا الحساب معطل حالياً. يرجى التواصل مع إدارة الكلية.", "This account is currently deactivated. Please contact administration."),
        ["Auth.AccountNotApproved"] = ("حسابك في انتظار موافقة المرشد الأكاديمي أو سكرتارية الكلية.", "Your account is currently pending approval by your academic advisor."),
        ["User.PendingApproval"] = ("حسابك في انتظار موافقة المرشد الأكاديمي أو سكرتارية الكلية.", "Your account is currently pending approval by your academic advisor."),
        ["Auth.DuplicateEmail"] = ("البريد الإلكتروني مُسجّل بالفعل في النظام.", "This email address is already registered."),
        ["User.EmailAlreadyExists"] = ("البريد الإلكتروني مُسجّل بالفعل في النظام.", "This email address is already registered."),
        ["Auth.DuplicateUniversityCode"] = ("الرقم الجامعي مُسجّل بالفعل.", "This university code is already registered."),
        ["User.UniversityCodeAlreadyExists"] = ("الرقم الجامعي مُسجّل بالفعل.", "This university code is already registered."),
        ["Auth.DuplicateNationalId"] = ("الرقم القومي مُسجّل بالفعل.", "This national ID is already registered."),
        ["User.NationalIdAlreadyExists"] = ("الرقم القومي مُسجّل بالفعل.", "This national ID is already registered."),
        ["User.AlreadyApproved"] = ("تمت الموافقة على هذا الطالب سابقاً.", "Student account is already approved."),
        ["Auth.InvalidRefreshToken"] = ("جلسة التسجيل غير صالحة، يرجى إعادة تسجيل الدخول.", "Invalid session. Please log in again."),
        ["Auth.RefreshTokenInactive"] = ("انتهت صلاحية الجلسة، يرجى تسجيل الدخول مجدداً.", "Session expired. Please log in again."),
        ["Auth.RegistrationFailed"] = ("تعذر تسجيل الحساب. يرجى التأكد من البيانات المدخلة.", "Account registration failed. Please check your details."),
        ["Auth.ChangePasswordFailed"] = ("تعذر تغيير كلمة المرور. يرجى التأكد من كلمة المرور الحالية.", "Password change failed. Please verify your current password."),
        ["Auth.DuplicateAdvisorCode"] = ("كود المرشد الأكاديمي مُسجّل بالفعل.", "Advisor code is already registered."),
        ["Advisor.EmptyList"] = ("قائمة المرشدين الأكاديميين فارغة.", "Advisor list cannot be empty."),

        // Request Errors
        ["Request.NotFound"] = ("عفواً، لم يتم العثور على الطلب.", "The requested application could not be found."),
        ["RequestNotFound"] = ("عفواً، لم يتم العثور على الطلب.", "The requested application could not be found."),
        ["Request.InvalidStatusForReview"] = ("لا يمكن مراجعة الطلب في حالته الحالية.", "This request cannot be reviewed in its current status."),
        ["Request.InvalidStatus"] = ("حالة الطلب غير صالحة لتنفيذ هذا الإجراء.", "Invalid request status for this action."),
        ["Request.AlreadyProcessed"] = ("تم اتخاذ قرار بشأن هذا الطلب سابقاً.", "This request has already been processed."),
        ["Request.InvalidStatusForSendEmail"] = ("لا يمكن إرسال الطلب للإدارة في حالته الحالية.", "This request cannot be sent to administration in its current status."),
        ["Email.SendFailed"] = ("تعذر إرسال البريد الإلكتروني للإدارة. يرجى التحقق من العنوان والمحاولة لاحقاً.", "Failed to send email to administration. Please check the email address and try again."),
        ["Request.InvalidForAdministrationConfirm"] = ("عفواً، تم اتخاذ قرار بشأن هذا الطلب أو أنه في حالة لا تسمح بالتأكيد.", "This request has already been processed or cannot be confirmed now."),
        ["Request.InvalidExternalAdministrationOtp"] = ("رمز التحقق غير صحيح أو انتهت صلاحيته.", "Verification code is invalid or has expired."),
        ["Request.InvalidStatusForWithdraw"] = ("لا يمكن سحب الطلب لأنه قيد المعالجة بالفعل أو تم الفصل فيه.", "Cannot withdraw this request as it is already being processed or completed."),
        ["Request.InvalidStatusForAdminOverride"] = ("لا يمكن تغيير حالة الطلب مباشرة إلى هذه الحالة.", "Cannot change status directly to this state."),
        ["Student.NotFound"] = ("عفواً، لم يتم العثور على بيانات الطالب.", "Student details could not be found."),
        ["Student.NotApproved"] = ("حسابك الأكاديمي ما زال قيد الموافقة، لا يمكنك تقديم طلبات حالياً.", "Your academic account is pending approval. You cannot submit requests yet."),
        ["Request.GpaTooLow"] = ("الحد الأدنى للمعدل التراكمي (GPA) هو 3.00 لتسجيل ساعات إضافية.", "A minimum GPA of 3.00 is required for extra credit hours."),
        ["Request.UnauthorizedAccess"] = ("عفواً، ليس لديك صلاحية للاطلاع على هذا الطلب.", "You are not authorized to view this request."),

        // Form Errors
        ["Form.NotFound"] = ("نموذج الطلب غير موجود.", "Form definition not found."),
        ["Form.Closed"] = ("عفواً، التقديم على هذا النموذج مغلق حالياً.", "Submissions for this form are currently closed."),
        ["Form.FieldNotFound"] = ("حقل النموذج غير موجود.", "Form field not found."),
        ["Form.RequiredFieldMissing"] = ("الحقل الإجباري [{0}] مفقود أو فارغ.", "The required field [{0}] is missing."),
        ["Form.InvalidFieldType"] = ("القيمة المدخلة للحقل [{0}] غير صالحة.", "The provided value for field [{0}] is invalid."),

        // Advisor Assignment Errors
        ["Advisor.NotFound"] = ("لم يتم العثور على المرشد الأكاديمي.", "Academic advisor not found."),
        ["Assignment.NotFound"] = ("لا يوجد توزيع مرشد أكاديمي لهذا الرقم الجامعي.", "No advisor assignment found for this university code."),
        ["Advisor.NoAdvisorsFound"] = ("لا يوجد مرشدين أكاديميين مسجلين للنظام، يرجى إضافة مرشد أولاً.", "No academic advisors found. Please create advisor accounts first."),

        // FluentValidation Rule Messages
        ["الاسم الأول باللغة العربية مطلوب."] = ("يرجى إدخال الاسم الأول باللغة العربية.", "Please enter your first name in Arabic."),
        ["اسم العائلة باللغة العربية مطلوب."] = ("يرجى إدخال اسم العائلة باللغة العربية.", "Please enter your last name in Arabic."),
        ["English First Name is required."] = ("يرجى إدخال الاسم الأول باللغة الإنجليزية.", "Please enter your first name in English."),
        ["English Last Name is required."] = ("يرجى إدخال اسم العائلة باللغة الإنجليزية.", "Please enter your last name in English."),
        ["الرقم الجامعي مطلوب."] = ("يرجى إدخال الرقم الجامعي.", "Please enter your university code."),
        ["الرقم القومي مطلوب."] = ("يرجى إدخال الرقم القومي.", "Please enter your national ID."),
        ["الرقم القومي يجب أن يتكون من 14 رقم فقط."] = ("الرقم القومي يجب أن يتكون من 14 رقماً.", "National ID must be exactly 14 digits."),
        ["البريد الإلكتروني غير صحيح."] = ("يرجى إدخال بريد إلكتروني صحيح.", "Please enter a valid email address."),
        ["البريد الإلكتروني مطلوب وسليم."] = ("يرجى إدخال بريد إلكتروني صحيح.", "Please enter a valid email address."),
        ["رقم الهاتف مطلوب."] = ("يرجى إدخال رقم الهاتف.", "Please enter your phone number."),
        ["العنوان مطلوب."] = ("يرجى إدخال العنوان.", "Please enter your address."),
        ["كلمة المرور مطلوبة."] = ("يرجى إدخال كلمة المرور.", "Please enter your password."),
        ["كلمة المرور يجب ألا تقل عن 6 أحرف."] = ("كلمة المرور يجب ألا تقل عن 6 أحرف.", "Password must be at least 6 characters long."),
        ["الاسم الكامل باللغة العربية مطلوب."] = ("يرجى إدخال الاسم الكامل باللغة العربية.", "Please enter your full name in Arabic."),
        ["الاسم العربي يجب أن يتكون من كلمتين على الأقل (الاسم الأول واسم العائلة)."] = ("الاسم العربي يجب أن يتكون من كلمتين على الأقل.", "Arabic name must contain at least two words."),
        ["Full English Name is required."] = ("يرجى إدخال الاسم الكامل باللغة الإنجليزية.", "Please enter your full name in English."),
        ["English name must contain at least two parts (First Name and Last Name)."] = ("الاسم الإنجليزي يجب أن يتكون من كلمتين على الأقل.", "English name must contain at least two words."),
        ["سبب الرفض مطلوب عند رفض الطلب."] = ("يرجى توضيح سبب الرفض.", "Please provide a reason for rejection."),
        ["معرف المرشد الأكاديمي مطلوب."] = ("يرجى اختيار المرشد الأكاديمي.", "Please select an academic advisor."),
        ["قائمة الأكواد الجامعية لا يمكن أن تكون فارغة."] = ("يرجى إدخال كود جامعي واحد على الأقل.", "Please provide at least one university code."),
        ["يجب تحديد النموذج (FormDefinitionId) المراد التقديم عليه."] = ("يرجى اختيار النموذج المراد التقديم عليه.", "Please select a valid form to submit.")
    };

    public LocalizationService(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
    }

    public string GetLocalizedString(string key)
    {
        return GetLocalizedString(key, Array.Empty<object>());
    }

    public string GetLocalizedString(string key, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        var localized = _localizer[key];
        if (!localized.ResourceNotFound && !string.IsNullOrWhiteSpace(localized.Value))
        {
            return args.Length > 0 ? string.Format(localized.Value, args) : localized.Value;
        }

        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase);

        if (_fallbackTranslations.TryGetValue(key, out var val))
        {
            var text = isArabic ? val.Ar : val.En;
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        return key;
    }
}
