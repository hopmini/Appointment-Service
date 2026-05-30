using System.Net;
using System.Net.Mail;

namespace AppointmentService.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Lấy thông tin từ cấu hình
                var smtpEmail = _configuration["SMTP_USERNAME"];
                var smtpPass = _configuration["SMTP_PASSWORD"];
                var smtpHost = _configuration["SMTP_HOST"];
                var smtpPortStr = _configuration["SMTP_PORT"];
                var fromName = _configuration["SMTP_FROM_NAME"] ?? "Medicare Hospital";

                if (string.IsNullOrEmpty(smtpEmail) || string.IsNullOrEmpty(smtpPass) || 
                    string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortStr))
                {
                    Console.WriteLine("[WARNING] SMTP Configuration is missing or incomplete. Email sending is skipped.");
                    return;
                }

                if (!int.TryParse(smtpPortStr, out int smtpPort))
                {
                    Console.WriteLine($"[WARNING] Invalid SMTP port value '{smtpPortStr}'. Email sending is skipped.");
                    return;
                }

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(smtpEmail, smtpPass);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"[SUCCESS] Email sent to {toEmail}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send email to {toEmail}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[INNER ERROR] {ex.InnerException.Message}");
                }
            }
        }
    }
}
