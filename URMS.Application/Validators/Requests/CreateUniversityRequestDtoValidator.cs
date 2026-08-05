using FluentValidation;
using URMS.Application.DTOs.Requests;

namespace URMS.Application.Validators.Requests;

public class CreateUniversityRequestDtoValidator : AbstractValidator<CreateUniversityRequestDto>
{
    public CreateUniversityRequestDtoValidator()
    {
        RuleFor(x => x.FormDefinitionId)
            .GreaterThan(0).WithMessage("يجب تحديد النموذج (FormDefinitionId) المراد التقديم عليه.");
    }
}
