namespace URMS.Domain.Enums;

public enum RequestType
{
    /// <summary>
    /// تسجيل الساعات كاملة لمرة واحدة — معدل تراكمي من 1.95 وحتى أقل من 2
    /// </summary>
    FullHoursRegistration = 0,

    /// <summary>
    /// تسجيل ساعات إضافية — معدل تراكمي من 3.3 وحتى أقل من 3.75
    /// </summary>
    ExtraHoursRegistration = 1,

    /// <summary>
    /// طلب شهادة إثبات قيد
    /// </summary>
    EnrollmentCertificate = 2,

    /// <summary>
    /// طلب كشف درجات / بيان درجات
    /// </summary>
    AcademicTranscript = 3,

    /// <summary>
    /// طلب عذر أو انسحاب من مقرر
    /// </summary>
    CourseWithdrawal = 4,

    /// <summary>
    /// طلب التماس إعادة تصحيح / إعادة رصد
    /// </summary>
    GradeAppeal = 5,

    /// <summary>
    /// طلب جامعي آخر مخصص
    /// </summary>
    Other = 6
}
