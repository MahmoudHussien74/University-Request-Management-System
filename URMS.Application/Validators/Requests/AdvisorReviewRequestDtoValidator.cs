using FluentValidation;
using URMS.Application.DTOs.Requests;

namespace URMS.Application.Validators.Requests;

public class AdvisorReviewRequestDtoValidator : AbstractValidator<AdvisorReviewRequestDto>
{
    public AdvisorReviewRequestDtoValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().When(x => !x.IsApproved)
            .WithMessage("سبب الرفض مطلوب عند رفض الطلب.");
    }
}
