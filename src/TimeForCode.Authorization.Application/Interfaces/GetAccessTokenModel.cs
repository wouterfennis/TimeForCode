namespace TimeForCode.Authorization.Application.Interfaces
{
    public class GetAccessTokenModel
    {
        public required string Host { get; init; }
        public required string ClientId { get; init; }
        public required string ClientSecret { get; init; }
        public required string Code { get; init; }
    }
}
