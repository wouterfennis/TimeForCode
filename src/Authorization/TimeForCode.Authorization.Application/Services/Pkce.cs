using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace TimeForCode.Authorization.Application.Services
{
    internal static class Pkce
    {
        /// <summary>
        /// The S256 code challenge (RFC 7636): base64url of the SHA-256 hash of the verifier, without padding.
        /// </summary>
        public static string CreateS256Challenge(string codeVerifier)
        {
            return Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        }
    }
}