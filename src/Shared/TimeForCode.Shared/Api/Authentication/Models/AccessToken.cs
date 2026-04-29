namespace TimeForCode.Shared.Api.Authentication.Models
{
    public class AccessToken
    {
        public required string Token { get; init; }

        public required DateTimeOffset ExpiresAfter { get; init; }
    }
}