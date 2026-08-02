using Mapster;
using URMS.Application.DTOs.Auth;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Entities;

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
            .Map(dest => dest.RequestType, src => src.Type.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.AdvisorName, src => src.Advisor != null ? src.Advisor.FullNameAr : null)
            .Map(dest => dest.StaffName, src => src.Staff != null ? src.Staff.FullNameAr : null);

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
    }
}
