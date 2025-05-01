using MCPClient.MCPClientServices;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class PasswordResetEventHandler : EmailNotificationHandlerBase<PasswordResetEvent>
{
    public PasswordResetEventHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<PasswordResetEventHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string, string, string, string) GetEmailData(PasswordResetEvent e)
    {
        return (
            e.Email,
            e.NewPassword,
            "🔒 Your Password Has Been Reset",
            $"""
            Write a plain text email. Follow these strict rules:

            1. Confirm that the password has been successfully reset.
            2. Include the following information:
               - Username: [EMAIL]
               - New Password: [PASSWORD]
            3. Avoid words like "sorry", "issue", or anything implying a problem.
            4. Do **not** offer advice or instructions.
            5. Do **not** include HTML tags or formatting.
            6. Use emojis to maintain a friendly tone.
            7. Return only the plain text message (no additional formatting or details).
            8. Only return the plain text message. No extra content.
            """
        );
    }

    protected override string GetFooter() => "Stay secure and take care! 😊";
}