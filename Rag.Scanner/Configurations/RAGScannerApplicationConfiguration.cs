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

        // Register MediatR Handlers automatically
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Only register "real" services: exclude classes without constructors or only properties
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.Where(type =>
                !type.Name.EndsWith("Command") &&  // ❌ exclude Command classes
                !type.Name.EndsWith("Query") &&    // ❌ optionally exclude Query classes
                !type.IsAssignableTo(typeof(IRequest<>)) && // ❌ extra safe
                !type.IsAssignableTo(typeof(IRequest))      // ❌ extra safe
            ))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }
}
