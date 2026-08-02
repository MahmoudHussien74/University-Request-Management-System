using FluentValidation;
using URMS.Application.DTOs.AdvisorAssignment;

namespace URMS.Application.Validators.AdvisorAssignment;

public class BulkAssignStudentsDtoValidator : AbstractValidator<BulkAssignStudentsDto>
{
    public BulkAssignStudentsDtoValidator()
    {
        RuleFor(x => x.AdvisorId).NotEmpty().WithMessage("معرف المرشد الأكاديمي مطلوب.");
        RuleFor(x => x.UniversityCodes).NotEmpty().WithMessage("قائمة الأكواد الجامعية لا يمكن أن تكون فارغة.");
    }
}
