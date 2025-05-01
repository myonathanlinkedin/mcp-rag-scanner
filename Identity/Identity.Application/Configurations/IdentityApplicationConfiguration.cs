using System.Reflection;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class IdentityApplicationConfiguration
{
    public static IServiceCollection AddIdentityApplicationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
              => services.AddCommonApplication(
                configuration,
                Assembly.GetExecutingAssembly());
}
