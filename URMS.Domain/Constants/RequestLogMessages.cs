namespace URMS.Domain.Constants;

public static class RequestLogMessages
{
    public const string CreatedByStudent = "تم إنشاء الطلب بواسطة الطالب";
    public const string ApprovedByAdvisor = "موافقة المرشد الأكاديمي";
    public const string RejectedByAdvisor = "رفض المرشد الأكاديمي";
    public const string ConfirmedByStaff = "الاعتماد النهائي من شؤون الطلاب";
    public const string RejectedByStaff = "الرفض من شؤون الطلاب";
    public const string AdminOverride = "تعديل إداري مباشر (SuperAdmin Override)";
}
