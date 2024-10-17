namespace TimeForCode.Authorization.Application.Interfaces
{
    public class GetAccessTokenResult
    {
        public required string AccessToken { get; init; }
        public required string Scope { get; init; }
        public required string TokenType { get; init; }
    }
}