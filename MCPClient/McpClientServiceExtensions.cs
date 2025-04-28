using MCPClient.MCPClientServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using OpenAI;
using OpenTelemetry.Logs;
using System.ClientModel;

public static class McpClientServiceExtensions
{
    public static IServiceCollection AddMcpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var llmModel = configuration["API:LlmModel"]!;

        // Register OpenAI Client
        services.AddScoped(sp =>
        {
            var apiKey = configuration["API:ApiKey"]!;
            var endpoint = configuration["API:Endpoint"]!;
            var apiKeyCredential = new ApiKeyCredential(apiKey);
            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };

            return new OpenAIClient(apiKeyCredential, clientOptions);
        });

        // Register IChatClient
        services.AddScoped<IChatClient>(sp =>
        {
            var openAIClient = sp.GetRequiredService<OpenAIClient>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var chatClient = openAIClient.GetChatClient(llmModel)
                .AsIChatClient()
                .AsBuilder()
                .UseLogging(loggerFactory: loggerFactory)
                .Build() as IChatClient;

            return chatClient ?? throw new InvalidCastException("SamplingChatClient build failed.");
        });

        // Register IChatClient
        services.AddScoped<IChatClient>(sp =>
        {
            var openAIClient = sp.GetRequiredService<OpenAIClient>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return openAIClient.GetChatClient(llmModel)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .UseLogging(loggerFactory: loggerFactory)
                .Build();
        });

        // Register SseClientTransport
        services.AddScoped(sp =>
        {
            var serverEndpoint = configuration["MCP:Endpoint"]!;
            var serverName = configuration["MCP:ServerName"]!;
            var uri = new Uri($"{serverEndpoint}/sse");

            return new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = uri,
                Name = serverName,
                ConnectionTimeout = TimeSpan.FromMinutes(1)
            });
        });

        // Register IMcpClient
        services.AddScoped<IMcpClient>(sp =>
        {
            var transport = sp.GetRequiredService<SseClientTransport>();
            var samplingClient = sp.GetRequiredService<IChatClient>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return McpClientFactory.CreateAsync(
                transport,
                clientOptions: new()
                {
                    Capabilities = new()
                    {
                        Sampling = new() { SamplingHandler = samplingClient.CreateSamplingHandler() }
                    }
                },
                loggerFactory: loggerFactory).GetAwaiter().GetResult();
        });

        // Register McpClientTools
        services.AddScoped<IEnumerable<McpClientTool>>(sp =>
        {
            var mcpClient = sp.GetRequiredService<IMcpClient>();
            return mcpClient.ListToolsAsync().GetAwaiter().GetResult();
        });

        // Register MCPServerRequester
        services.AddSingleton<IChatMessageStore, ChatMessageStore>();
        services.AddScoped<IList<ChatMessage>, List<ChatMessage>>();
        services.AddScoped<IMCPServerRequester, MCPServerRequester>();

        return services;
    }
}
