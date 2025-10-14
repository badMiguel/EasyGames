using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}

public interface IEmailService
{
    Task SendEmailAsync(IEnumerable<string?> toList, string subject, string body);
}

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;

    public SmtpEmailService(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(IEnumerable<string?> toList, string subject, string body)
    {
        if (!toList.Any())
        {
            throw new ArgumentException("Recipient list is empty", nameof(toList));
        }

        using var message = new MailMessage();
        message.From = new MailAddress(_settings.From);
        message.To.Add(new MailAddress(_settings.From));
        foreach (var to in toList)
        {
            if (to != null)
            {
                message.Bcc.Add(new MailAddress(to));
            }
        }
        message.Subject = subject;
        message.IsBodyHtml = true;
        message.Body = body;

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
        };

        await client.SendMailAsync(message, default).ConfigureAwait(false);
    }
}
