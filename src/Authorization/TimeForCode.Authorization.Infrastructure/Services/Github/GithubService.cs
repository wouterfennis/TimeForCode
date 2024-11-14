using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using System.Net.Mime;
using System.Text.Json;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services.Github
{
    internal class GithubService : IIdentityProviderService
    {
        private const string UserEndpoint = "/user";
        private readonly ExternalIdentityProvider _identityProviderOptions;
        private readonly RestClient _restClient;

        public GithubService(IOptions<ExternalIdentityProviderOptions> options, RestClient restClient)
        {
            _identityProviderOptions = options.Value.GetExternalIdentityProvider(IdentityProvider.Github);
            _restClient = restClient;
        }

        public async Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.Host,
                Path = OAuthConstants.AccessTokenEndpoint
            };

            uriBuilder.Port = _identityProviderOptions.HostPort ?? uriBuilder.Port;

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Post);
            request.AddBody(new
            {
                client_id = _identityProviderOptions.ClientId,
                client_secret = _identityProviderOptions.ClientSecret,
                code
            });

            var response = await _restClient.ExecuteAsync<GetAccessTokenResult>(request);

            if (response.IsSuccessful)
            {
                return Result<GetAccessTokenResult>.Success(response.Data!);
            }

            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(response.Content!);

            return Result<GetAccessTokenResult>.Failure(problemDetails!.Detail);
        }

        public async Task<Result<AccountInformation>> GetAccountInformation(ExternalAccessToken accessToken)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.RestApiHost,
                Path = UserEndpoint,
                Port = _identityProviderOptions.RestApiPort ?? -1
            };

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Get);
            request.AddHeader(OAuthConstants.AuthorizationHeader, $"{OAuthConstants.BearerPrefix}{accessToken.Token}");

            var response = await _restClient.ExecuteAsync<GithubUser>(request);

            if (response.IsSuccessful)
            {
                var accountInformation = response.Data!.MapToAccountInformation();
                return Result<AccountInformation>.Success(accountInformation);
            }

            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(response.Content!);

            return Result<AccountInformation>.Failure(problemDetails!.Detail);
        }

        public Task<TokenValidationParameters> GetTokenValidationParameters()
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _identityProviderOptions.Issuer,
                ValidateAudience = true,
                ValidAudiences = [_identityProviderOptions.ClientId],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false
            };

            return Task.FromResult(tokenValidationParameters);
        }
    }
}