using URMS.Application.DTOs.Auth;

namespace URMS.Application.Mappings;

public class MapsterRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // UniversityRequest -> UniversityRequestResponseDto mapping
        config.NewConfig<UniversityRequest, UniversityRequestResponseDto>()
            .Map(dest => dest.StudentNameAr, src => src.Student.FullNameAr)
            .Map(dest => dest.StudentNameEn, src => src.Student.FullNameEn)
            .Map(dest => dest.UniversityCode, src => src.Student.Student != null ? src.Student.Student.UniversityCode : null)
            .Map(dest => dest.FormTitleAr, src => src.FormDefinition != null ? src.FormDefinition.TitleAr : null)
            .Map(dest => dest.FormTitleEn, src => src.FormDefinition != null ? src.FormDefinition.TitleEn : null)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.AdvisorName, src => src.Advisor != null ? src.Advisor.FullNameAr : null)
            .Map(dest => dest.ExternalAdministrationEmail, src => src.ExternalAdministrationEmail)
            .Map(dest => dest.IsExternalAdministrationNotificationSent, src => !string.IsNullOrWhiteSpace(src.ExternalAdministrationEmail) && !string.IsNullOrWhiteSpace(src.ConfirmationToken) && src.ExternalAdministrationSentAt.HasValue)
            .Map(dest => dest.ExternalAdministrationSentAt, src => src.ExternalAdministrationSentAt)
            .Map(dest => dest.ExternalAdministrationOtpExpiresAt, src => src.ExternalAdministrationOtpExpiresAt)
            .Map(dest => dest.ExternalAdministrationRespondedAt, src => src.ExternalAdministrationRespondedAt)
            .Map(dest => dest.ExternalAdministrationResponseNotes, src => src.ExternalAdministrationResponseNotes);
        // ApplicationUser -> PendingStudentDto mapping
        config.NewConfig<ApplicationUser, PendingStudentDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FullNameAr, src => src.FullNameAr)
            .Map(dest => dest.FullNameEn, src => src.FullNameEn)
            .Map(dest => dest.Email, src => src.Email!)
            .Map(dest => dest.UniversityCode, src => src.Student != null ? src.Student.UniversityCode : "")
            .Map(dest => dest.NationalId, src => src.Student != null ? src.Student.NationalId : "")
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
            .Map(dest => dest.Address, src => src.Student != null ? src.Student.Address : "")
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        // ApplicationUser -> StudentActivationDto mapping
        config.NewConfig<ApplicationUser, StudentActivationDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FullNameAr, src => src.FullNameAr)
            .Map(dest => dest.FullNameEn, src => src.FullNameEn)
            .Map(dest => dest.Email, src => src.Email!)
            .Map(dest => dest.UniversityCode, src => src.Student != null ? src.Student.UniversityCode : null)
            .Map(dest => dest.NationalId, src => src.Student != null ? src.Student.NationalId : null)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
            .Map(dest => dest.GPA, src => src.Student != null ? src.Student.GPA : null)
            .Map(dest => dest.IsApproved, src => src.IsApproved)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}
