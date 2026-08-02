using FluentValidation;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Enums;

namespace URMS.Application.Validators.Requests;

public class CreateUniversityRequestDtoValidator : AbstractValidator<CreateUniversityRequestDto>
{
    public CreateUniversityRequestDtoValidator()
    {
        RuleFor(x => x.RequestType)
            .IsInEnum().WithMessage("نوع الطلب غير معروف.");

        RuleFor(x => x.Gpa)
            .InclusiveBetween(0.0m, 4.0m).When(x => x.Gpa.HasValue)
            .WithMessage("المعدل التراكمي يجب أن يكون بين 0 و 4.0");

        RuleFor(x => x.RequestedHours)
            .InclusiveBetween(1, 30).When(x => x.RequestedHours.HasValue)
            .WithMessage("عدد الساعات المطلوبة يجب أن يكون بين 1 و 30.");
    }
}
