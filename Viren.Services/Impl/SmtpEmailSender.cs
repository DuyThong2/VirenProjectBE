using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Viren.Services.Interfaces;

namespace Viren.Services.Impl
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            var smtpHost = _config["Email:Smtp:Host"];
            var smtpPort = int.Parse(_config["Email:Smtp:Port"]!);
            var smtpUser = _config["Email:Smtp:Username"];
            var smtpPass = _config["Email:Smtp:Password"];
            var from = _config["Email:Smtp:From"];

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            using var message = new MailMessage(from!, toEmail)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, ct);

            _logger.LogInformation("Email sent to {Email}", toEmail);
        }
    }
}
