using System.Net;
using System.Net.Mail;

namespace LibraryAPI.Services
{
    public class EmailService
    {
        private readonly string _fromEmail;
        private readonly string _password;

        public EmailService()
        {
            _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
            _password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")!;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_fromEmail, _password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, "LibraryAPI"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true  // HTML email bhej sakte hain
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}