using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class RAGScannerInfrastructureExtensions
{
    public static IServiceCollection AddRAGScannerInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient();

        var assembly = Assembly.GetExecutingAssembly();

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses() // <- select all classes
            .AsImplementedInterfaces() // <- map to all interfaces
            .WithScopedLifetime() // <- scoped lifetime
        );

        return services;
    }
}