namespace URMS.Domain.Enums;

public enum RequestStatus
{
    /// <summary>
    /// الطلب مقدم من الطالب — في انتظار مراجعة المرشد الأكاديمي
    /// </summary>
    Pending = 0,

    /// <summary>
    /// المرشد الأكاديمي يراجع الطلب ويؤكد صحة المعدل
    /// </summary>
    UnderAdvisorReview = 1,

    /// <summary>
    /// المرشد الأكاديمي وافق وأكد صحة المعدل
    /// </summary>
    AdvisorApproved = 2,

    /// <summary>
    /// تم إرسال إيميل لمسؤول شؤون الطلاب مع رابط التأكيد
    /// </summary>
    SentToStaff = 3,

    /// <summary>
    /// مسؤول شؤون الطلاب أكد عبر رابط الإيميل
    /// </summary>
    StaffConfirmed = 4,

    /// <summary>
    /// في انتظار سداد الرسوم (للساعات الإضافية فقط)
    /// </summary>
    PendingPayment = 5,

    /// <summary>
    /// تم تنفيذ الطلب بنجاح
    /// </summary>
    Completed = 6,

    /// <summary>
    /// الطلب مرفوض
    /// </summary>
    Rejected = 7
}
