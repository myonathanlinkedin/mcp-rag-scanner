using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services
   .AddIdentityApplication(builder.Configuration)
   .AddIdentityInfrastructure(builder.Configuration)
   .AddIdentityWebComponents()
   .AddTokenAuthentication(builder.Configuration)
   .AddEventSourcing()
   .AddModelBinders()
   .AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Web API", Version = "v1" }))
   .AddHttpClient()
   .AddMcpClient(builder.Configuration)
   .AddMemoryCache()
   .AddDistributedMemoryCache()
   .AddSession(options =>
   {
       options.Cookie.Name = ".Prompting.Session";
       options.Cookie.HttpOnly = true;
       options.Cookie.IsEssential = true;
       options.Cookie.SameSite = SameSiteMode.Lax;
       options.IdleTimeout = TimeSpan.FromMinutes(30);
   })
   .AddHttpContextAccessor();

Log.Logger = new LoggerConfiguration()
   .WriteTo.Console()
   .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
   .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var app = builder.Build();

// Ensure cookies are only sent over HTTPS
app.UseCookiePolicy(new CookiePolicyOptions
{
    Secure = CookieSecurePolicy.Always // This ensures cookies are sent only over HTTPS
});

app
   .UseHttpsRedirection()
   .UseSession()
   .UseWebService(app.Environment)
   .Initialize();

app.Run();
