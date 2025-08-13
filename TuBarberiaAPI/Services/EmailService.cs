using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace TuBarberiaAPI.Services
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _logger = logger;

            // Valores por defecto + overrides desde configuration/env
            _smtpServer = config["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(config["Email:SmtpPort"], out var p) ? p : 587;
            _smtpUser = config["Email:SmtpUser"] ?? "reserbyte@gmail.com";

            // Orden de lectura: AppSettings -> variable "EMAIL_SMTP_PASS"
            _smtpPass = config["Email:SmtpPass"]
                        ?? Environment.GetEnvironmentVariable("EMAIL_SMTP_PASS");

            if (string.IsNullOrWhiteSpace(_smtpPass))
                _logger.LogError("SMTP password no configurado. Configure 'Email:SmtpPass' o 'EMAIL_SMTP_PASS'.");
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("TuBarbería", _smtpUser));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Correo enviado a {To} con asunto {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo enviando correo a {To} con asunto {Subject}", to, subject);
                throw; // deja que el controlador lo vea (para que no “falle silencioso”)
            }
        }
    }
}
