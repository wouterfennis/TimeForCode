using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Api.V1
{
    /// <summary>
    /// Maps the claims of the signed-in caller to Donation values.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        private const string ScopeClaim = "scope";
        private const string AdminScope = "admin";

        /// <summary>
        /// Builds the <see cref="Actor"/> of the caller: the platform user id and whether the caller holds the administrator scope.
        /// </summary>
        /// <param name="user">The signed-in caller.</param>
        /// <param name="actor">The actor, or <c>null</c> when the claims hold no user id.</param>
        /// <returns><c>true</c> when the user id could be determined.</returns>
        public static bool TryGetActor(this ClaimsPrincipal user, [NotNullWhen(true)] out Actor? actor)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                actor = null;
                return false;
            }

            actor = new Actor(userId, user.HasClaim(ScopeClaim, AdminScope));
            return true;
        }
    }
}