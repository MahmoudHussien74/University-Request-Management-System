using URMS.Domain.Abstractions;

namespace URMS.Application.Contracts.Requests;

public interface IRequestNotificationService
{
    /// <summary>
    /// Sends an external administration email for a request.
    /// Parameters are primitive types for Hangfire serialization compatibility.
    /// Called as a background job — do NOT await the result in the HTTP pipeline.
    /// </summary>
    Task SendExternalAdministrationEmailAsync(
        int requestId,
        string administrationEmail,
        string? advisorMessage,
        string otpCode,
        string reviewLink);
}
