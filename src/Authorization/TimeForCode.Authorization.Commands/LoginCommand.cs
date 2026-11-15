using MediatR;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Commands
{
    public class LoginCommand : IRequest<Uri>
    {
        public required IdentityProvider IdentityProvider { get; init; }
        public required Uri RedirectUri { get; init; }

        /// <summary>
        /// The S256 PKCE challenge of the client that starts the login; the code exchange later requires its verifier.
        /// </summary>
        public required string CodeChallenge { get; init; }
    }
}