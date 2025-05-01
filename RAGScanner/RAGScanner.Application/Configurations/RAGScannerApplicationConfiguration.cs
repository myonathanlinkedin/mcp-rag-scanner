using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class RAGScannerApplicationConfiguration
{
    public static IServiceCollection AddRAGScannerApplication(
        this IServiceCollection services,
        IConfiguration configuration)
             => services.AddCommonApplication(
                        configuration,
                        Assembly.GetExecutingAssembly());
}
