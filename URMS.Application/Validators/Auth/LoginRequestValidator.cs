using FluentValidation;
using URMS.Application.DTOs.Auth;

namespace URMS.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty()
        .EmailAddress().WithMessage("البريد الإلكتروني مطلوب وسليم.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة.");
    }
}
