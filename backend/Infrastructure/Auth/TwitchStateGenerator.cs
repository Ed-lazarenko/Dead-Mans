using System.Security.Cryptography;

namespace backend.Infrastructure.Auth;

public static class TwitchStateGenerator
{
    public static string Create()
    {
        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
