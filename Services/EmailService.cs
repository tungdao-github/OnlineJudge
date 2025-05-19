using System.Net;
using System.Net.Mail;

namespace OnlineJudgeAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var smtpHost = _config["SmtpSettings:Host"];
            var smtpPort = _config["SmtpSettings:Port"];
            var smtpUsername = _config["SmtpSettings:User"];
            var smtpPassword = _config["SmtpSettings:Pass"];
            var smtpFrom = _config["SmtpSettings:From"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPort) ||
                string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(smtpFrom))
            {
                throw new InvalidOperationException("SMTP configuration is missing required values.");
            }

            using var smtpClient = new SmtpClient(smtpHost)
            {
                Port = int.Parse(smtpPort),
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpFrom),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(to);
            await smtpClient.SendMailAsync(mailMessage);
        }

    }
}