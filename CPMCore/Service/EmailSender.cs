using FacadeCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CPMCore.Service
{
    public class SmtpEmailSender : IEmailSender
    {
        // Vangt <img src="data:image/...;base64,...">. Browsers (en dus onze eigen preview)
        // tonen data-URI's gewoon, maar Gmail/Outlook en vele spamfilters knippen ze weg uit
        // binnenkomende mails. Daarom zetten we ze hier om naar echte inline CID-bijlagen,
        // wat wél universeel ondersteund wordt in e-mailclients.
        private static readonly Regex DataImageRegex = new(
            "src=(?<q>[\"'])data:image/(?<type>[a-zA-Z0-9.+-]+);base64,(?<data>[A-Za-z0-9+/=\\s]+)\\k<q>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            string? bcc = null,
            string? fromEmail = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("To e-mail is verplicht.", nameof(toEmail));

            var message = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(fromEmail) ? _smtpUser : fromEmail),
                Subject = subject
            };

            var cleanedHtml = WordHtmlSanitizer.Clean(htmlMessage);
            var (processedHtml, inlineImages) = InlineDataImages(cleanedHtml);
            if (inlineImages.Count > 0)
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(processedHtml, Encoding.UTF8, "text/html");
                foreach (var resource in inlineImages)
                    htmlView.LinkedResources.Add(resource);
                message.AlternateViews.Add(htmlView);
            }
            else
            {
                message.Body = processedHtml;
                message.IsBodyHtml = true;
            }

            message.To.Add(new MailAddress(toEmail));

            if (!string.IsNullOrWhiteSpace(cc))
            {
                foreach (var address in cc.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.CC.Add(new MailAddress(address));
                }
            }
            if (!string.IsNullOrWhiteSpace(bcc))
            {
                foreach (var address in bcc.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.Bcc.Add(new MailAddress(address));
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

        private static (string Html, List<LinkedResource> Resources) InlineDataImages(string? html)
        {
            var resources = new List<LinkedResource>();
            if (string.IsNullOrEmpty(html))
                return (html ?? string.Empty, resources);

            var index = 0;
            var result = DataImageRegex.Replace(html, match =>
            {
                try
                {
                    var mimeSubtype = match.Groups["type"].Value.ToLowerInvariant();
                    var base64 = Regex.Replace(match.Groups["data"].Value, @"\s+", string.Empty);
                    var bytes = Convert.FromBase64String(base64);

                    index++;
                    var contentId = $"embimg{index}_{Guid.NewGuid():N}";

                    var resource = new LinkedResource(new MemoryStream(bytes, writable: false), "image/" + mimeSubtype)
                    {
                        ContentId = contentId,
                        TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                    };
                    resources.Add(resource);

                    var quote = match.Groups["q"].Value;
                    return $"src={quote}cid:{contentId}{quote}";
                }
                catch
                {
                    // Kon de data-URI niet verwerken: laat de originele src ongewijzigd staan.
                    return match.Value;
                }
            });

            return (result, resources);
        }
    }
}
