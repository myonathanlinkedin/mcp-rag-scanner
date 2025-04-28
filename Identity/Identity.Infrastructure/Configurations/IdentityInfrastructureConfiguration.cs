using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class IdentityInfrastructureConfiguration
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentity(configuration)
                .AddDBStorage<IdentityDbContext>(configuration, Assembly.GetExecutingAssembly())
                .AddIdentityAssemblyServices();

        // Register RsaKeyProviderService as a singleton
        services.AddSingleton<IRsaKeyProvider, RsaKeyProviderService>();

        return services;
    }

    private static IServiceCollection AddIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var resetTokenExpirationSeconds = configuration["ApplicationSettings:ResetTokenExpirationSeconds"];
        if (!int.TryParse(resetTokenExpirationSeconds, out int resetTokenExpiration))
        {
            resetTokenExpiration = 300; // default 5 minutes
        }

        services
            .AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = CommonModelConstants.Identity.MinPasswordLength;

                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
            })
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<IdentityDbContext>();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromSeconds(resetTokenExpiration);
        });

        return services;
    }

    private static IServiceCollection AddIdentityAssemblyServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.InNamespaceOf<IdentityService>()) // <- better: typesafe
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }
}
