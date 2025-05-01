using MCPClient.MCPClientServices;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class UserRegisteredEventHandler : EmailNotificationHandlerBase<UserRegisteredEvent>
{
    public UserRegisteredEventHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<UserRegisteredEventHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string, string, string, string) GetEmailData(UserRegisteredEvent e)
    {
        return (
            e.Email,
            e.Password,
            "🎉 Hooray! Your Account is Ready 🎉",
            $"""
            Write a plain text email. Follow these strict rules:

            1. Greet the user with positivity.
            2. Confirm that the account has been successfully created.
            3. Include the following information:
               - Username: [EMAIL]
               - Your Password: [PASSWORD]
            4. Avoid words like "sorry", "issue", or anything implying a problem.
            5. Do **not** offer advice or instructions.
            6. Do **not** include HTML tags or formatting.
            7. Use emojis to maintain a friendly tone.
            8. Return only the plain text message (no additional formatting or details).
            9. Only return the plain text message. No extra content.
            """
        );
    }

    protected override string GetFooter() => "Welcome aboard! 😊";
}