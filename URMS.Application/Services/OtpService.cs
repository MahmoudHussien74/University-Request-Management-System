using System.Text;

namespace URMS.Application.Services;

public class OtpService : IOtpService
{
    public string GenerateOtpCode(int length = 6)
    {
        const string digits = "0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => digits[b % digits.Length]).ToArray());
    }

    public string HashOtp(string otp, string saltKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(saltKey));
        var bytes = Encoding.UTF8.GetBytes(otp);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyOtp(string otp, string saltKey, string hash)
    {
        var computedHash = HashOtp(otp, saltKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash));
    }
}
