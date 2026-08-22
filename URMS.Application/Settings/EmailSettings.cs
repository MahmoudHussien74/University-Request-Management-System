namespace URMS.Application.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = default!;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string SmtpUser { get; set; } = default!;
    public string SmtpPassword { get; set; } = default!;
    public string FromName { get; set; } = "URMS";
    public string FromAddress { get; set; } = default!;
    public string ExternalAdministrationBaseUrl { get; set; } = default!;
    public int ExternalAdministrationOtpTtlMinutes { get; set; } = 2880;
}
