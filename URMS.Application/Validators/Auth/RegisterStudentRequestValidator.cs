using FluentValidation;
using URMS.Application.DTOs.Auth;

namespace URMS.Application.Validators.Auth;

public class RegisterStudentRequestValidator : AbstractValidator<RegisterStudentRequest>
{
    public RegisterStudentRequestValidator()
    {
        RuleFor(x => x.FirstNameAr).NotEmpty().WithMessage("الاسم الأول باللغة العربية مطلوب.");
        RuleFor(x => x.LastNameAr).NotEmpty().WithMessage("اسم العائلة باللغة العربية مطلوب.");
        RuleFor(x => x.FirstNameEn).NotEmpty().WithMessage("English First Name is required.");
        RuleFor(x => x.LastNameEn).NotEmpty().WithMessage("English Last Name is required.");
        RuleFor(x => x.UniversityCode).NotEmpty().WithMessage("الرقم الجامعي مطلوب.");
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("الرقم القومي مطلوب.")
            .Matches(@"^\d{14}$").WithMessage("الرقم القومي يجب أن يتكون من 14 رقم فقط.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("البريد الإلكتروني غير صحيح.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الهاتف مطلوب.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("العنوان مطلوب.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("كلمة المرور يجب ألا تقل عن 6 أحرف.");
    }
}
