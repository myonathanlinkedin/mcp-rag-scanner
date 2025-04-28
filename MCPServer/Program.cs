using MCPServer.Tools;
using Serilog;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var config = BuildConfiguration();
string serverName = config["MCP:ServerName"];

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new SerilogLoggerProvider(Log.Logger));

// Register HttpClient for dependency injection
builder.Services.AddHttpClient<APITools>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<APITools>();

var app = builder.Build();

app.MapMcp();

app.Run();

Log.CloseAndFlush();

static IConfiguration BuildConfiguration()
{
    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
}
