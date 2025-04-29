using System.Security.Cryptography;
using System.Text;

public class HashHelper
{
    public static string ComputeDeterministicGuid(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);

        // Take first 16 bytes and create a Guid
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        var guid = new Guid(guidBytes);
        return guid.ToString(); // returns string like "123e4567-e89b-12d3-a456-426614174000"
    }
}
