namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRestService
    {
        public Task<string> GetAccessTokenAsync(GetAccessTokenModel model);
    }
}
