using Microsoft.IdentityModel.Tokens;

public interface IJwtGenerator
{
    Task<string> GenerateToken(User user);
    JsonWebKey GetPublicKey();
}