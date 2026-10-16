using System.Security.Cryptography;
using System.Text;

namespace TimeForCode.Authorization.Application.Services
{
    /// <summary>
    /// Derives the persisted form of a refresh token. Tokens are high-entropy random strings,
    /// so a plain SHA-256 digest is sufficient and keeps the lookup deterministic.
    /// </summary>
    public static class RefreshTokenHasher
    {
        public static string Hash(string token)
        {
            byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexStringLower(digest);
        }
    }
}