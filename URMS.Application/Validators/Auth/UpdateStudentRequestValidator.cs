using FluentValidation;
using URMS.Application.DTOs.Auth;

namespace URMS.Application.Validators.Auth;

public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.FullNameAr)
            .NotEmpty().WithMessage("الاسم الكامل باللغة العربية مطلوب.")
            .Must(name => name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .WithMessage("الاسم العربي يجب أن يتكون من كلمتين على الأقل (الاسم الأول واسم العائلة).");

        RuleFor(x => x.FullNameEn)
            .NotEmpty().WithMessage("Full English Name is required.")
            .Must(name => name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .WithMessage("English name must contain at least two parts (First Name and Last Name).");

        RuleFor(x => x.UniversityCode).NotEmpty().WithMessage("الرقم الجامعي مطلوب.");
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("الرقم القومي مطلوب.")
            .Matches(@"^\d{14}$").WithMessage("الرقم القومي يجب أن يتكون من 14 رقم فقط.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("البريد الإلكتروني غير صحيح.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الهاتف مطلوب.");
    }
}
