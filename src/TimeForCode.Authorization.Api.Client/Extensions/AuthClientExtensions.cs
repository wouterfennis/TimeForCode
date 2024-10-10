using System.Net;

namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public static class AuthClientExtensions
    {
        public static async Task<RedirectResult> LoginWithRedirectAsync(this IAuthClient client, LoginModel model)
        {
            try
            {
                await client.LoginAsync(model);
            }
            catch (ApiException exception)
            {
                return new RedirectResult
                {
                    Url = exception.Headers["Location"].Single()
                };
            }

            throw new ApiException("Expected redirect but did not occur.", (int)HttpStatusCode.InternalServerError, null, null, null);
        }
    }
}
