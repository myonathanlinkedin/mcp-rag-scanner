using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class RAGScannerInfrastructureConfiguration
{
    public static IServiceCollection AddRAGScannerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR Handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Dynamically register all services and their interfaces from the assembly
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.Where(type => type.IsClass))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }
}
