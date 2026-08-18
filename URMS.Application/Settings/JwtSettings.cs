namespace URMS.Application.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int AccessTokenExpirationInMinutes { get; set; } = 30;
    public int RefreshTokenExpirationInDays { get; set; } = 7;
}
