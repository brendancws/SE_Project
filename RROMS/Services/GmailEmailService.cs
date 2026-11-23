using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RROMS.Models;

namespace RROMS.Services
{
    public class GmailEmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public GmailEmailService(
            IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody)
        {
            var message =
                new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _settings.FromName,
                    _settings.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

            message.Subject =
                subject;

            message.Body =
                new BodyBuilder
                {
                    HtmlBody = htmlBody
                }.ToMessageBody();

            using var client =
                new SmtpClient();

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }
    }
}