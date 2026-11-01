using System.Security.Cryptography;
using System.Text;

namespace IdentityProviderMockService.Models
{
    /// <summary>
    /// Derives the stable GitHub-like identity of a persona. Without a persona the mock keeps its original user.
    /// </summary>
    public static class PersonaProfile
    {
        public const string ClaimType = "persona";

        private const uint DefaultId = 1;
        private const string DefaultLogin = "johndoe";
        private const uint FirstPersonaId = 1000;

        public const int MaxLength = 64;
        public const string AllowedPattern = "^[A-Za-z0-9._ -]*$";

        public static uint IdFor(string? persona)
        {
            if (string.IsNullOrWhiteSpace(persona))
            {
                return DefaultId;
            }

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(persona)));
            return FirstPersonaId + (BitConverter.ToUInt32(hash, 0) % (uint.MaxValue - FirstPersonaId));
        }

        public static string LoginFor(string? persona) =>
            string.IsNullOrWhiteSpace(persona) ? DefaultLogin : Normalize(persona);

        // Id and login share one normalisation so "Alice" and "alice " are the same user.
        private static string Normalize(string persona) => persona.Trim().ToLowerInvariant();
    }
}