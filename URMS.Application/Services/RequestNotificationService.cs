using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using URMS.Application.Contracts.Infrastructure;
using URMS.Application.Contracts.Persistence;
using URMS.Application.Contracts.Requests;
using URMS.Domain.Entities;

namespace URMS.Application.Services;

/// <summary>
/// Handles email notifications as Hangfire background jobs.
/// Loads request data from DB independently since it runs outside the original HTTP scope.
/// </summary>
public class RequestNotificationService : IRequestNotificationService
{
    private readonly IEmailService _emailService;
    private readonly IUniversityRequestRepository _requestRepo;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<RequestNotificationService> _logger;

    public RequestNotificationService(
        IEmailService emailService,
        IUniversityRequestRepository requestRepo,
        IOptions<EmailSettings> emailOptions,
        ILogger<RequestNotificationService> logger)
    {
        _emailService = emailService;
        _requestRepo = requestRepo;
        _emailSettings = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendExternalAdministrationEmailAsync(
        int requestId,
        string administrationEmail,
        string? advisorMessage,
        string otpCode,
        string reviewLink)
    {
        // Load request from DB (this runs in a background scope, not the original HTTP request)
        var request = await _requestRepo.GetForAdministrationSendAsync(requestId);
        if (request is null)
        {
            _logger.LogWarning("Background email job: Request {RequestId} not found — may have been deleted", requestId);
            return;
        }

        Dictionary<string, string>? additionalData = null;
        if (!string.IsNullOrEmpty(request.AdditionalDataJson))
        {
            try
            {
                additionalData = JsonSerializer.Deserialize<Dictionary<string, string>>(request.AdditionalDataJson);
            }
            catch (JsonException)
            {
                additionalData = null;
            }
        }

        var studentName = request.Student?.FullNameEn ?? "Student";
        var formTitle = request.FormDefinition?.TitleEn ?? "University Request";
        var answersHtml = BuildAdditionalDataHtml(request.FormDefinition, additionalData);
        var subject = $"New student request for administration - {studentName}";
        var body = $@"
            <p>Hello,</p>
            <p>A new student request has been submitted for your review by the academic advisor.</p>
            <p><strong>Student Name:</strong> {studentName}</p>
            <p><strong>Request Type:</strong> {formTitle}</p>
            {answersHtml}
            <p><strong>Advisor Message:</strong> {WebUtility.HtmlEncode(advisorMessage ?? string.Empty)}</p>
            <p><strong>Verification Code:</strong> {otpCode}</p>
            <p>This code expires after {_emailSettings.ExternalAdministrationOtpTtlMinutes} minutes.</p>
            <p>You may review and respond to this request using the following link:</p>
            <p><a href='{reviewLink}'>Review & Respond to Request</a></p>
        ";

        await _emailService.SendEmailAsync(administrationEmail, subject, body);

        _logger.LogInformation(
            "External administration email sent for request {RequestId} to {Email}",
            requestId, administrationEmail);
    }

    private static string BuildAdditionalDataHtml(FormDefinition? formDefinition, Dictionary<string, string>? additionalData)
    {
        if (additionalData is null || !additionalData.Any())
            return string.Empty;

        var rows = new List<string>();
        foreach (var kvp in additionalData)
        {
            var fieldLabel = formDefinition?.Fields?.FirstOrDefault(f => f.FieldKey == kvp.Key)?.LabelEn ?? kvp.Key;
            var fieldValue = WebUtility.HtmlEncode(kvp.Value);
            rows.Add($"<tr><td style='padding:8px;border:1px solid #ddd;'><strong>{fieldLabel}</strong></td><td style='padding:8px;border:1px solid #ddd;'>{fieldValue}</td></tr>");
        }

        return $@"
            <p><strong>Student Answers:</strong></p>
            <table style='border-collapse:collapse;width:100%;margin-bottom:16px;'>
                {string.Join("", rows)}
            </table>
        ";
    }
}
