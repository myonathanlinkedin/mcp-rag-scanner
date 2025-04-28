using System.Reflection;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class RAGScannerApplicationConfiguration
{
    public static IServiceCollection AddRAGScannerApplication(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // ⬇️ Register ApplicationSettings
        var appSettings = configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();
        services.AddSingleton(appSettings);

        // Register MediatR Handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Scan and register other services
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.Where(type =>
                !type.Name.EndsWith("Command") &&
                !type.Name.EndsWith("Query") &&
                !type.IsAssignableTo(typeof(IRequest<>)) &&
                !type.IsAssignableTo(typeof(IRequest))
            ))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }
}
