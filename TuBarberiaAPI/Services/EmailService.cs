using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using System;

namespace TuBarberiaAPI.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser = "reserbyte@gmail.com";
        private readonly string _smtpPass = Environment.GetEnvironmentVariable("EMAIL_SMTP_PASS")!;

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("TuBarberia", _smtpUser));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}