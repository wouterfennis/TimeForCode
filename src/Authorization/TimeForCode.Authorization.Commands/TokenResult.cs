namespace TimeForCode.Authorization.Commands
{
    public class TokenResult
    {
        public required Values.AccessToken InternalAccessToken { get; init; }
        public required Values.RefreshToken RefreshToken { get; init; }
    }
}