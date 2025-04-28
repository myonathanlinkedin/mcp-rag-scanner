using MCPClient.MCPClientServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Text;

public class MCPServerRequester : IMCPServerRequester
{
    private readonly IChatClient chatClient;
    private readonly IEnumerable<McpClientTool> tools;
    private readonly ILogger<MCPServerRequester> logger;
    private readonly IChatMessageStore messageStore;
    private readonly IHttpContextAccessor httpContextAccessor;

    private readonly string sessionId;

    public MCPServerRequester(IChatClient chatClient, IEnumerable<McpClientTool> tools, IChatMessageStore messageStore, ILogger<MCPServerRequester> logger, IHttpContextAccessor httpContextAccessor)
    {
        this.chatClient = chatClient;
        this.tools = tools;
        this.logger = logger;
        this.messageStore = messageStore;
        this.httpContextAccessor = httpContextAccessor;

        // Try to get the SessionId from cookies or session. If not found, generate a new one
        sessionId = httpContextAccessor.HttpContext?.Request?.Cookies["SessionId"]
                    ?? httpContextAccessor.HttpContext?.Session?.Id
                    ?? Guid.NewGuid().ToString();

        // Set the SessionId cookie if it is not already set
        SetSessionCookie(sessionId);
    }

    private void SetSessionCookie(string sessionId)
    {
        if (httpContextAccessor.HttpContext != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContextAccessor.HttpContext.Request.IsHttps,  // Ensure cookie is secure in HTTPS
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.Now.AddDays(1)  // Set expiration for 1 day
            };

            // Only set the cookie if it's not already set
            if (httpContextAccessor.HttpContext.Request.Cookies["SessionId"] == null)
            {
                httpContextAccessor.HttpContext.Response.Cookies.Append("SessionId", sessionId, cookieOptions);
            }
        }
    }

    public async Task<Result<string>> RequestAsync(string prompt)
    {
        try
        {
            // Retrieve messages from the message store
            var messages = messageStore.GetMessages(sessionId);

            // Add the new user message
            messages.Add(new ChatMessage(ChatRole.User, prompt));

            List<ChatResponseUpdate> updates = new List<ChatResponseUpdate>();

            // Call to get streaming response
            var results = chatClient.GetStreamingResponseAsync(messages, new() { Tools = tools.Cast<AITool>().ToList() });

            StringBuilder responseBuilder = new StringBuilder();

            await foreach (var update in results)
            {
                responseBuilder.Append(update);
                updates.Add(update);
            }

            // Save updates as new chat responses
            messages.AddMessages(updates);

            // Save the updated message list back to the store
            messageStore.SaveMessages(sessionId, messages);

            return Result<string>.SuccessWith(responseBuilder.ToString());
        }
        catch (Exception ex)
        {
            // Log the exception
            logger.LogError(ex, "An error occurred while processing the request.");

            // Return a failure result with the error message
            return Result<string>.Failure(new List<string> { $"An error occurred: {ex.Message}" });
        }
    }
}
