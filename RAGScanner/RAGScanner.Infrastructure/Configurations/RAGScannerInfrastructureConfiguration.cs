using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class RAGScannerInfrastructureConfiguration
{
    public static IServiceCollection AddRAGScannerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
             => services.AddRAGScannerAssemblyServices();

    private static IServiceCollection AddRAGScannerAssemblyServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.InNamespaceOf<DocumentParserService>()) // <- better: typesafe
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }
}
