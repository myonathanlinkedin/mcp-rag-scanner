using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class ChangePasswordEventHandler : EmailNotificationHandlerBase<PasswordChangedEvent>
{
    public ChangePasswordEventHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<ChangePasswordEventHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string, string, string, string) GetEmailData(PasswordChangedEvent e)
    {
        return (
            e.Email,
            e.NewPassword,
            "✅ Your Password Was Successfully Changed",
            $"""
            Write a plain text email. Follow these strict rules:

            1. Greet the user with positivity.
            2. Confirm that their password has been successfully changed.
            3. Include the following information:
               - Username: [EMAIL]
               - New Password: [PASSWORD]
            4. If this action was not initiated by them, advise them to reach out.
            5. Avoid words like "sorry", "issue", or anything implying a problem.
            6. Do **not** offer advice or instructions.
            7. Do **not** include HTML tags or formatting.
            8. Use emojis to maintain a friendly tone.
            9. Only return the plain text message. No extra content.
            """
        );
    }

    protected override string GetFooter() => "Thanks for keeping your account secure! 🔐";
}
