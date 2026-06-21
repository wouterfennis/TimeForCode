using DnsClient.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using System.Net.Mime;
using System.Text.Json;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Models;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services.Github
{
    internal class GithubService : IIdentityProviderService, IGithubApiService
    {
        private const string UserEndpoint = "/user";
        private const string UserReposEndpoint = "/user/repos";
        private const string ReposEndpoint = "/repos";
        private readonly ExternalIdentityProvider _identityProviderOptions;
        private readonly IOptions<ExternalIdentityProviderOptions> _options;
        private readonly RestClient _restClient;
        private readonly ILogger<GithubService> _logger;

        public GithubService(IOptions<ExternalIdentityProviderOptions> options,
            RestClient restClient,
            ILogger<GithubService> logger)
        {
            _identityProviderOptions = options.Value.GetExternalIdentityProvider(IdentityProvider.Github);
            _options = options;
            _restClient = restClient;
            _logger = logger;
        }

        public async Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.AccessTokenHost,
                Path = OAuthConstants.AccessTokenEndpoint,
                Scheme = _identityProviderOptions.IsHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
            };

            uriBuilder.Port = _identityProviderOptions.AccessTokenHostPort ?? uriBuilder.Port;

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            _logger.LogDebug("Sending access token request towards: {Uri}", uriBuilder);

            var request = new RestRequest(uriBuilder.ToString(), Method.Post);
            request.AddHeader("Content-Type", MediaTypeNames.Application.Json);
            request.AddJsonBody(new
            {
                client_id = _identityProviderOptions.ClientId,
                client_secret = _identityProviderOptions.ClientSecret,
                code = code,
                redirect_uri = _options.Value.CallbackUri
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
                Scheme = _identityProviderOptions.IsHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
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

            return Result<AccountInformation>.Failure(response.Content!);
        }

        public async Task<Result<IEnumerable<RepositoryInfo>>> GetUserRepositoriesAsync(string githubAccessToken)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.RestApiHost,
                Path = UserReposEndpoint,
                Scheme = _identityProviderOptions.IsHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
            };

            uriBuilder.Port = _identityProviderOptions.RestApiPort ?? uriBuilder.Port;

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Get);
            request.AddHeader(OAuthConstants.AuthorizationHeader, $"{OAuthConstants.BearerPrefix}{githubAccessToken}");

            var response = await _restClient.ExecuteAsync<List<GithubRepository>>(request);

            if (response.IsSuccessful)
            {
                var repositories = response.Data!
                    .Where(r => !r.IsPrivate)
                    .Select(r => new RepositoryInfo
                    {
                        Name = r.Name,
                        Description = r.Description,
                        StarCount = r.StargazersCount,
                        Language = r.Language,
                        Url = r.HtmlUrl
                    });
                return Result<IEnumerable<RepositoryInfo>>.Success(repositories);
            }

            return Result<IEnumerable<RepositoryInfo>>.Failure(response.Content ?? $"GitHub API returned {(int)response.StatusCode}", response.StatusCode);
        }

        public async Task<Result<RepositoryInfo>> GetRepositoryAsync(string owner, string repo, string githubAccessToken)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.RestApiHost,
                Path = $"{ReposEndpoint}/{owner}/{repo}",
                Scheme = _identityProviderOptions.IsHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
            };

            uriBuilder.Port = _identityProviderOptions.RestApiPort ?? uriBuilder.Port;

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Get);
            request.AddHeader(OAuthConstants.AuthorizationHeader, $"{OAuthConstants.BearerPrefix}{githubAccessToken}");

            var response = await _restClient.ExecuteAsync<GithubRepository>(request);

            if (response.IsSuccessful)
            {
                var repositoryInfo = new RepositoryInfo
                {
                    Name = response.Data!.Name,
                    Description = response.Data.Description,
                    StarCount = response.Data.StargazersCount,
                    Language = response.Data.Language,
                    Url = response.Data.HtmlUrl
                };
                return Result<RepositoryInfo>.Success(repositoryInfo);
            }

            return Result<RepositoryInfo>.Failure(
                response.Content ?? $"GitHub API returned {(int)response.StatusCode}",
                response.StatusCode);
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
                ValidateIssuerSigningKey = true
            };

            return Task.FromResult(tokenValidationParameters);
        }
    }
}