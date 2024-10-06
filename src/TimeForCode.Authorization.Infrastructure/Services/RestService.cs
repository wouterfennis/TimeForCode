using RestSharp;
using System.Net.Mime;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class RestService : IRestService
    {
        public async Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(GetAccessTokenModel model)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = model.Host,
            };

            var client = new RestClient(uriBuilder.ToString());
            client.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest("/login/oauth/access_token",Method.Post);
            request.AddBody(new
            {
                client_id = model.ClientId,
                client_secret = model.ClientSecret,
                code = model.Code
            });

            var response = await client.ExecuteAsync<GetAccessTokenResult>(request);

            if(response.IsSuccessful){
                return Result<GetAccessTokenResult>.Success(response.Data!);
            }

            return Result<GetAccessTokenResult>.Failure(response.ErrorMessage!);
        }
    }
}
