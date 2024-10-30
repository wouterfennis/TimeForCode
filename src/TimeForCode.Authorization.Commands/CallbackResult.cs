namespace TimeForCode.Authorization.Commands
{
    public class CallbackResult
    {
        public required Values.AccessToken InternalAccessToken { get; init; }
        public required Values.RefreshToken RefreshToken { get; init; }
    }
}