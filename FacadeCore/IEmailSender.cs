namespace FacadeCore;

public interface IEmailSender
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlMessage,
        IEnumerable<EmailAttachment>? attachments = null,
        string? cc = null,
        string? bcc = null,
        string? fromEmail = null);
}

public sealed record EmailAttachment(string FileName, byte[] Content, string ContentType);
