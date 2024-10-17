using Microsoft.Extensions.Options;
using RestSharp;
using System.Net.Mime;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class GithubService : IIdentityProviderService
    {
        private readonly ExternalIdentityProvider _identityProviderOptions;

        public GithubService(IOptions<ExternalIdentityProviderOptions> options)
        {
            _identityProviderOptions = options.Value.GetExternalIdentityProvider(IdentityProvider.Github);
        }

        public async Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = _identityProviderOptions.Host,
            };

            var client = new RestClient(uriBuilder.ToString());
            client.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest("/login/oauth/access_token", Method.Post);
            request.AddBody(new
            {
                client_id = _identityProviderOptions.ClientId,
                client_secret = _identityProviderOptions.ClientSecret,
                code = code
            });

            var response = await client.ExecuteAsync<GetAccessTokenResult>(request);

            if (response.IsSuccessful)
            {
                return Result<GetAccessTokenResult>.Success(response.Data!);
            }

            return Result<GetAccessTokenResult>.Failure(response.ErrorMessage!);
        }

        public async Task<Result<AccountInformation>> GetAccountInformation(GetAccountInformationModel model)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = _identityProviderOptions.RestApiHost,
            };

            var client = new RestClient(uriBuilder.ToString());
            client.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest("/user", Method.Get);

            var response = await client.ExecuteAsync<AccountInformation>(request);

            if (response.IsSuccessful)
            {
                return Result<AccountInformation>.Success(response.Data!);
            }

            return Result<AccountInformation>.Failure(response.ErrorMessage!);
        }
    }
}
