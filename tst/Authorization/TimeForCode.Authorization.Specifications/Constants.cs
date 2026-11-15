using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace TimeForCode.Authorization.Specifications
{
    internal static class Constants
    {
        public const string StateKey = "StateKey";
        public const string CodeVerifier = "code-verifier-0123456789-0123456789-0123456789";
        public const string AdminBootstrapSecret = "test-bootstrap-secret";

        /// <summary>
        /// The PKCE verifier of the client that signs in; only the client holds it, the platform only sees its challenge.
        /// </summary>
        public const string ClientCodeVerifier = "client-verifier-0123456789-0123456789-0123456789";

        public static readonly string ClientCodeChallenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(ClientCodeVerifier)));
    }
}