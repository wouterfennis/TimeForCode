using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// Opaque random ids for the session and login cookies. Only their SHA-256 hash is stored.
    /// </summary>
    public static class SessionIds
    {
        public static string Create() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        public static string Hash(string id) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)));
    }
}