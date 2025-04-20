using DnsClient.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GithubService> _logger;

        public GithubService(IOptions<ExternalIdentityProviderOptions> options,
            RestClient restClient,
            ILogger<GithubService> logger)
        {
            _identityProviderOptions = options.Value.GetExternalIdentityProvider(IdentityProvider.Github);
            _restClient = restClient;
            _logger = logger;
        }

        public async Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code, Uri redirectUri)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.AccessTokenHost,
                Path = OAuthConstants.AccessTokenEndpoint,
                Scheme = Uri.UriSchemeHttps
            };

            uriBuilder.Port = _identityProviderOptions.AccessTokenHostPort ?? uriBuilder.Port;

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            _logger.LogDebug("Sending access token request towards: {Uri}", uriBuilder.ToString());

            var request = new RestRequest(uriBuilder.ToString(), Method.Post);
            request.AddHeader("Content-Type", MediaTypeNames.Application.Json);
            request.AddJsonBody(new
            {
                client_id = _identityProviderOptions.ClientId,
                client_secret = _identityProviderOptions.ClientSecret,
                code = code,
                redirect_uri = redirectUri.ToString()
            });

            var response = await _restClient.ExecuteAsync<GetAccessTokenResult>(request);

            if (response.IsSuccessful)
            {
                return Result<GetAccessTokenResult>.Success(response.Data!);
            }

            return Result<GetAccessTokenResult>.Failure(response.Content!);
        }

        public async Task<Result<AccountInformation>> GetAccountInformation(ExternalAccessToken accessToken)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.RestApiHost,
                Path = UserEndpoint,
            };

            uriBuilder.Port = _identityProviderOptions.RestApiPort ?? uriBuilder.Port;

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