using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
        => services
            .AddIdentity(configuration)
            .AddDBStorage<IdentityDbContext>(configuration, Assembly.GetExecutingAssembly())
            .AddScoped<IDbInitializer, IdentityDbInitializer>(); // Scoped because it's a request-based service

    private static IServiceCollection AddIdentity(
      this IServiceCollection services,
      IConfiguration configuration) // Add IConfiguration as a parameter
    {
        // Get the ResetTokenExpirationSeconds value as a string
        var resetTokenExpirationSeconds = configuration["ApplicationSettings:ResetTokenExpirationSeconds"];

        // Parse it to an integer (you can handle it as needed)
        if (!int.TryParse(resetTokenExpirationSeconds, out int resetTokenExpiration))
        {
            // Set a default value if parsing fails (e.g., 300 seconds)
            resetTokenExpiration = 300;
        }

        services
            .AddScoped<IIdentity, IdentityService>() // Scoped for login/register/reset operations
            .AddScoped<IJwtGenerator, JwtGeneratorService>() // Scoped for token generation
            .AddSingleton<IRsaKeyProvider, RsaKeyProviderService>() // Singleton as it's stateless
            .AddScoped<IEmailSender, EmailSenderService>() // Scoped for sending emails during registration/reset
            .AddIdentity<User, IdentityRole>(options =>
            {
                // Password requirements
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = CommonModelConstants.Identity.MinPasswordLength;

                // Token providers
                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
            })
            .AddDefaultTokenProviders() // Adds the default token providers
            .AddEntityFrameworkStores<IdentityDbContext>(); // Set the EF Core store for Identity

        // Configure Data Protection Token Provider lifespan using the ResetTokenExpirationSeconds from configuration
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromSeconds(resetTokenExpiration); // Set lifespan using the parsed value
        });

        return services;
    }
}
