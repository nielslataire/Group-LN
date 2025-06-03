using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CPMCore.Service
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
    public class SmtpEmailSender : IEmailSender
    {
        private readonly string _smtpServer = "smtp.office365.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _smtpUser = configuration["EmailSettings:SmtpUser"]
                        ?? throw new ArgumentNullException("EmailSettings:SmtpUser");
            _smtpPass = configuration["EmailSettings:SmtpPass"]
                        ?? throw new ArgumentNullException("EmailSettings:SmtpPass");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_smtpUser),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}
