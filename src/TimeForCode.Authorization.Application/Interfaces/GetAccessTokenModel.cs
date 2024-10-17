namespace TimeForCode.Authorization.Application.Interfaces
{
    public class GetAccessTokenModel
    {
        public required string Code { get; init; }
    }
}