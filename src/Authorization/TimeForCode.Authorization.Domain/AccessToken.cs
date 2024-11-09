
namespace TimeForCode.Authorization.Domain
{
    public class AccessToken
    {
        public required string Token { get; init; }
        public DateTimeOffset ExpiresAfter { get; set; }
    }
}