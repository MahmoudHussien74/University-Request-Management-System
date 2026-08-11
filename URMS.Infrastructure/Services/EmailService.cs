using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using URMS.Domain.Settings;

using URMS.Application.Contracts.Infrastructure;

namespace URMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        using var message = new MailMessage();
        message.From = new MailAddress(_settings.FromAddress, _settings.FromName);
        message.To.Add(new MailAddress(toEmail));
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await client.SendMailAsync(message);
    }
}