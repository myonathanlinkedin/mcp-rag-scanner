using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

public class EmailSenderService : IEmailSender
{
    private readonly IConfiguration configuration;

    public EmailSenderService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpClient = new SmtpClient(configuration["MailHog:SmtpServer"])
        {
            Port = int.Parse(configuration["MailHog:SmtpPort"]),
            Credentials = new NetworkCredential("user", "password"), // You can leave this as default for MailHog
            EnableSsl = false
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(configuration["MailHog:FromAddress"]),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
