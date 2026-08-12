namespace URMS.Application.Contracts.Requests;

public interface IOtpService
{
    string GenerateOtpCode(int length = 6);
    string HashOtp(string otp, string saltKey);
    bool VerifyOtp(string otp, string saltKey, string hash);
}
