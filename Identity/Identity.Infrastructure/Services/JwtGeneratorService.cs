using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class JwtGeneratorService : IJwtGenerator
{
    private readonly UserManager<User> userManager;
    private readonly IRsaKeyProvider keyProvider;
    private readonly string audience;
    private readonly string issuer;
    private readonly int tokenExpirationSeconds;

    private const string RsaAlgorithm = SecurityAlgorithms.RsaSha256Signature;

    public JwtGeneratorService(
        UserManager<User> userManager,
        IOptions<ApplicationSettings> appSettings,
        IRsaKeyProvider keyProvider)
    {
        this.userManager = userManager;
        this.keyProvider = keyProvider;
        this.audience = appSettings.Value.Audience;
        this.issuer = appSettings.Value.Issuer;
        this.tokenExpirationSeconds = appSettings.Value.TokenExpirationSeconds;
    }

    public async Task<string> GenerateToken(User user)
    {
        var rsa = keyProvider.GetPrivateKey();
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email)
            }),
            Expires = DateTime.UtcNow.AddSeconds(tokenExpirationSeconds),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), RsaAlgorithm)
        };

        if (await userManager.IsInRoleAsync(user, CommonModelConstants.Common.AdministratorRoleName))
        {
            tokenDescriptor.Subject.AddClaim(new Claim(
                ClaimTypes.Role,
                CommonModelConstants.Common.AdministratorRoleName));
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public JsonWebKey GetPublicKey()
    {
        return keyProvider.GetPublicJwk();
    }
}
