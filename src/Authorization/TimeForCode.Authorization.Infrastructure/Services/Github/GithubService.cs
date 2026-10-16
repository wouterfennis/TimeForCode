using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Models;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Infrastructure.Services.Github
{
    internal partial class GithubService : IIdentityProviderService, IGithubApiService
    {
        private const string UserEndpoint = "/user";
        private const string UsersEndpoint = "/users";
        private const string MaxPageSize = "100";
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

        public async Task<Result<GetAccessTokenResult>> GetAccessTokenAsync(string code, string codeVerifier)
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
                code_verifier = codeVerifier,
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

        public async Task<Result<IEnumerable<RepositoryInfo>>> GetUserRepositoriesAsync(string login)
        {
            if (!IsSafePathSegment(login))
            {
                return Result<IEnumerable<RepositoryInfo>>.Failure("Invalid GitHub login.", HttpStatusCode.NotFound);
            }

            var request = CreatePublicDataRequest($"{UsersEndpoint}/{login}/repos");
            request.AddQueryParameter("type", "owner");
            request.AddQueryParameter("per_page", MaxPageSize);

            var response = await _restClient.ExecuteAsync<List<GithubRepository>>(request);

            if (response.IsSuccessful)
            {
                var repositories = response.Data!
                    .Where(r => !r.IsPrivate)
                    .Select(MapToRepositoryInfo);
                return Result<IEnumerable<RepositoryInfo>>.Success(repositories);
            }

            return Result<IEnumerable<RepositoryInfo>>.Failure(response.Content ?? $"GitHub API returned {(int)response.StatusCode}", response.StatusCode);
        }

        public async Task<Result<RepositoryInfo>> GetRepositoryAsync(string owner, string repo)
        {
            if (!IsSafePathSegment(owner) || !IsSafePathSegment(repo))
            {
                return Result<RepositoryInfo>.Failure("Invalid GitHub repository.", HttpStatusCode.NotFound);
            }

            var request = CreatePublicDataRequest($"{ReposEndpoint}/{owner}/{repo}");

            var response = await _restClient.ExecuteAsync<GithubRepository>(request);

            if (!response.IsSuccessful)
            {
                return Result<RepositoryInfo>.Failure(
                    response.Content ?? $"GitHub API returned {(int)response.StatusCode}",
                    response.StatusCode);
            }

            if (response.Data!.IsPrivate)
            {
                return Result<RepositoryInfo>.Failure("Repository is not public.", HttpStatusCode.NotFound);
            }

            return Result<RepositoryInfo>.Success(MapToRepositoryInfo(response.Data));
        }

        private RestRequest CreatePublicDataRequest(string path)
        {
            var uriBuilder = new UriBuilder
            {
                Host = _identityProviderOptions.RestApiHost,
                Path = path,
                Scheme = _identityProviderOptions.IsHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
            };

            uriBuilder.Port = _identityProviderOptions.RestApiPort ?? uriBuilder.Port;

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Get);

            // The client secret must never travel over plain HTTP; without it GitHub serves the same public data at the anonymous rate limit.
            if (_identityProviderOptions.IsHttps)
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_identityProviderOptions.ClientId}:{_identityProviderOptions.ClientSecret}"));
                request.AddHeader(OAuthConstants.AuthorizationHeader, $"{OAuthConstants.BasicPrefix}{credentials}");
            }

            return request;
        }

        /// <summary>
        /// Rejects values that could change the request path (separators, query characters, dot segments).
        /// </summary>
        private static bool IsSafePathSegment(string value) => SafePathSegment().IsMatch(value);

        [GeneratedRegex(@"^(?!\.+$)[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
        private static partial Regex SafePathSegment();

        private static RepositoryInfo MapToRepositoryInfo(GithubRepository repository)
        {
            return new RepositoryInfo
            {
                Name = repository.Name,
                Description = repository.Description,
                StarCount = repository.StargazersCount,
                Language = repository.Language,
                Url = repository.HtmlUrl
            };
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