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
    ExtraHoursRegistration = 1
}
