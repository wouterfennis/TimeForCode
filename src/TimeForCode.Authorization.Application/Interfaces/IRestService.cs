namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRestService
    {
        public Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(GetAccessTokenModel model);
    }
}
