using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Commands
{
    public class TokenResult
    {
        public required AccessToken InternalAccessToken { get; init; }
        public required Values.RefreshToken RefreshToken { get; init; }
        public Uri? RedirectUri { get; init; }
    }
}