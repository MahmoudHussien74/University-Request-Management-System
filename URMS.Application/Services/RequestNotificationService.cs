using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using URMS.Application.Contracts.Infrastructure;
using URMS.Application.Contracts.Requests;
using URMS.Application.DTOs.Requests;
using URMS.Domain.Abstractions;
using URMS.Domain.Constants;
using URMS.Domain.Entities;

namespace URMS.Application.Services;

public class RequestNotificationService : IRequestNotificationService
{
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;

    public RequestNotificationService(
        IEmailService emailService,
        IOptions<EmailSettings> emailOptions)
    {
        _emailService = emailService;
        _emailSettings = emailOptions.Value;
    }

    public async Task<Result> SendExternalAdministrationEmailAsync(
        UniversityRequest request,
        SendRequestToAdministrationDto dto,
        string otpCode,
        string reviewLink)
    {
        Dictionary<string, string>? additionalData = null;
        if (!string.IsNullOrEmpty(request.AdditionalDataJson))
        {
            try
            {
                additionalData = JsonSerializer.Deserialize<Dictionary<string, string>>(request.AdditionalDataJson);
            }
            catch
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
            <p><strong>Advisor Message:</strong> {WebUtility.HtmlEncode(dto.Message ?? string.Empty)}</p>
            <p><strong>Verification Code:</strong> {otpCode}</p>
            <p>This code expires after {_emailSettings.ExternalAdministrationOtpTtlMinutes} minutes.</p>
            <p>You may review and respond to this request using the following link:</p>
            <p><a href='{reviewLink}'>Review & Respond to Request</a></p>
        ";

        try
        {
            await _emailService.SendEmailAsync(dto.AdministrationEmail, subject, body);
            return Result.Success();
        }
        catch
        {
            return Result.Failure(RequestErrors.EmailSendingFailed);
        }
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
