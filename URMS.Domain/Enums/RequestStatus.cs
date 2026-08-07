namespace URMS.Domain.Enums;

public enum RequestStatus
{
    /// <summary>
    /// الطلب مقدم من الطالب — في انتظار مراجعة المرشد الأكاديمي
    /// </summary>
    Pending = 0,

    /// <summary>
    /// الطلب وافق عليه المرشد الأكاديمي (لم يُرسَل بعد للإدارة)
    /// </summary>
    AdvisorApproved = 1,

    /// <summary>
    /// الطلب أُرسِل إلى شؤون الطلاب / إدارة الجامعة في انتظار ردهم
    /// </summary>
    SentToAdministration = 2,

    /// <summary>
    /// الطلب تم تنفيذه بنجاح
    /// </summary>
    Completed = 3,

    /// <summary>
    /// الطلب مرفوض
    /// </summary>
    Rejected = 4
}
