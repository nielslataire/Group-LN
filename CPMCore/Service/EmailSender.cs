using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CPMCore.Service
{
    public interface IEmailSender
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage,
            IEnumerable<EmailAttachment>? attachments = null,
            string? cc = null,
            string? fromEmail = null);
    }

    public sealed record EmailAttachment(string FileName, byte[] Content, string ContentType);

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

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage,
            IEnumerable<EmailAttachment>? attachments = null,
            string? cc = null,
            string? fromEmail = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("To e-mail is verplicht.", nameof(toEmail));

            var message = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(fromEmail) ? _smtpUser : fromEmail),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail));

            if (!string.IsNullOrWhiteSpace(cc))
            {
                foreach (var address in cc.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.CC.Add(new MailAddress(address));
                }
            }

            if (attachments != null)
            {
                foreach (var attachment in attachments.Where(a => a != null))
                {
                    if (attachment.Content == null || string.IsNullOrWhiteSpace(attachment.FileName))
                        continue;

                    var stream = new MemoryStream(attachment.Content, writable: false);
                    var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                    message.Attachments.Add(mailAttachment);
                }
            }

            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}