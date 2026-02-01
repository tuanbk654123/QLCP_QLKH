using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace BE_QLKH.Services;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string title, string message, string link = "");
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string title, string message, string link = "")
    {
        if (string.IsNullOrEmpty(_settings.SenderEmail) || string.IsNullOrEmpty(_settings.Password))
        {
            _logger.LogWarning("Email settings are not configured. Skipping email sending.");
            Console.WriteLine("Email settings are not configured. Skipping email sending.");
            return;
        }

        try
        {
            _logger.LogInformation($"Attempting to send email to {toEmail} with subject: {subject}");
            Console.WriteLine($"Attempting to send email to {toEmail} with subject: {subject}");

            var password = _settings.Password.Replace(" ", ""); // Remove spaces from App Password
            var smtpClient = new SmtpClient(_settings.SmtpServer)
            {
                Port = _settings.SmtpPort,
                Credentials = new NetworkCredential(_settings.SenderEmail, password),
                EnableSsl = _settings.EnableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = GetHtmlTemplate(title, message, link),
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation($"Email sent successfully to {toEmail}");
            Console.WriteLine($"Email sent successfully to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}. Error: {ex.Message}");
            Console.WriteLine($"Failed to send email to {toEmail}. Error: {ex.Message}");
            Console.WriteLine(ex.ToString());
        }
    }

    private string GetHtmlTemplate(string title, string message, string link)
    {
        var buttonHtml = string.IsNullOrEmpty(link) 
            ? "" 
            : $@"<a href=""{link}"" style=""display: inline-block; padding: 10px 20px; background-color: #007bff; color: #ffffff; text-decoration: none; border-radius: 5px; font-weight: bold;"">Xem chi tiết</a>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; background-color: #f9f9f9; }}
        .header {{ text-align: center; padding-bottom: 20px; border-bottom: 1px solid #eee; }}
        .header h1 {{ color: #007bff; margin: 0; }}
        .content {{ padding: 20px 0; }}
        .footer {{ text-align: center; font-size: 12px; color: #777; border-top: 1px solid #eee; padding-top: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>QLKH Notification</h1>
        </div>
        <div class=""content"">
            <h2 style=""color: #333;"">{title}</h2>
            <div style=""font-size: 16px;"">{message}</div>
            <div style=""text-align: center; margin-top: 30px;"">
                {buttonHtml}
            </div>
        </div>
        <div class=""footer"">
            <p>Đây là email tự động, vui lòng không trả lời.</p>
            <p>&copy; {DateTime.Now.Year} QLKH System</p>
        </div>
    </div>
</body>
</html>";
    }
}
