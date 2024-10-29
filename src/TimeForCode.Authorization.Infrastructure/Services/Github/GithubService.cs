using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using System.Net.Mime;
using System.Text.Json;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;
using Microsoft.Extensions.Caching.Memory;

namespace TimeForCode.Authorization.Infrastructure.Services.Github
{
    internal class GithubService : IIdentityProviderService
    {
        private readonly ExternalIdentityProvider _identityProviderOptions;
        private readonly RestClient _restClient;
        private readonly IMemoryCache _memoryCache;

        public GithubService(IOptions<ExternalIdentityProviderOptions> options, RestClient restClient, IMemoryCache memoryCache)
        {
            _identityProviderOptions = options.Value.GetExternalIdentityProvider(IdentityProvider.Github);
            _restClient = restClient;
            _memoryCache = memoryCache;
        }

        public async Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.Host,
                Path = "/login/oauth/access_token"
            };

            uriBuilder.Port = _identityProviderOptions.HostPort.HasValue ? _identityProviderOptions.HostPort.Value : uriBuilder.Port;

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

        public async Task<Result<AccountInformation>> GetAccountInformation(GetAccountInformationModel model)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.RestApiHost,
                Path = "/user"
            };

            uriBuilder.Port = _identityProviderOptions.RestApiPort.HasValue ? _identityProviderOptions.RestApiPort.Value : uriBuilder.Port;

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Get);
            request.AddHeader("Authorization", $"Bearer {model.AccessToken}");

            var response = await _restClient.ExecuteAsync<GithubUser>(request);

            if (response.IsSuccessful)
            {
                var accountInformation = response.Data!.MapToAccountInformation();
                return Result<AccountInformation>.Success(accountInformation);
            }

            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(response.Content!);

            return Result<AccountInformation>.Failure(problemDetails!.Detail);
        }

        public async Task<TokenValidationParameters> GetTokenValidationParameters()
        {
            if (_memoryCache.TryGetValue(_identityProviderOptions.MetaDataAddress, out TokenValidationParameters? tokenValidationParameters))
            {
                return tokenValidationParameters!;
            }

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                _identityProviderOptions.MetaDataAddress,
                new OpenIdConnectConfigurationRetriever());

            var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = openIdConfig.Issuer,
                ValidateAudience = true,
                ValidAudiences = new List<string> { _identityProviderOptions.ClientId },
                ValidateLifetime = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateIssuerSigningKey = true
            };

            _memoryCache.Set(_identityProviderOptions.MetaDataAddress, tokenValidationParameters, TimeSpan.FromHours(1));

            return tokenValidationParameters;
        }
    }
}