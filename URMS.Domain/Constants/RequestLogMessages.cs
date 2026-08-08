namespace URMS.Domain.Constants;

public static class RequestLogMessages
{
    public const string CreatedByStudent = "تم إنشاء الطلب بواسطة الطالب";
    public const string ApprovedByAdvisor = "موافقة المرشد الأكاديمي";
    public const string RejectedByAdvisor = "رفض المرشد الأكاديمي";
    public const string ConfirmedByAdministration = "الاعتماد النهائي من شؤون الطلاب/الإدارة";
    public const string RejectedByAdministration = "الرفض من شؤون الطلاب/الإدارة";
    public const string WithdrawnByStudent = "سحب الطلب بواسطة الطالب";
    public const string SentToAdministration = "تم إرسال الطلب إلى شؤون الطلاب/الإدارة عبر البريد الإلكتروني";
    public const string ExternalAdministrationResponded = "تم الرد من شؤون الطلاب/الإدارة عبر الرابط الخارجي";
    public const string AdminOverride = "تعديل إداري مباشر من أدمن النظام";
}
