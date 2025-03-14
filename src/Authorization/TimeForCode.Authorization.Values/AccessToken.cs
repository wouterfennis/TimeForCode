namespace TimeForCode.Authorization.Values
{
    public class AccessToken
    {
        public required string Token { get; init; }

        public required DateTimeOffset ExpiresAfter { get; init; }
    }
}