﻿using Microsoft.Extensions.Options;
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
                Scheme = "https",
                Host = _identityProviderOptions.Host,
                Path = "/login/oauth/access_token"
            };

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Post);
            request.AddBody(new
            {
                client_id = _identityProviderOptions.ClientId,
                client_secret = _identityProviderOptions.ClientSecret,
                code = code
            });

            var response = await _restClient.ExecuteAsync<GetAccessTokenResult>(request);

            if (response.IsSuccessful)
            {
                return Result<GetAccessTokenResult>.Success(response.Data!);
            }

            return Result<GetAccessTokenResult>.Failure(response.ErrorMessage);
        }

        public async Task<Result<AccountInformation>> GetAccountInformation(GetAccountInformationModel model)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = _identityProviderOptions.RestApiHost,
                Path = "/user"
            };

            _restClient.AcceptedContentTypes = [MediaTypeNames.Application.Json];

            var request = new RestRequest(uriBuilder.ToString(), Method.Get);

            var response = await _restClient.ExecuteAsync<AccountInformation>(request);

            if (response.IsSuccessful)
            {
                return Result<AccountInformation>.Success(response.Data!);
            }

            return Result<AccountInformation>.Failure(response.ErrorMessage!);
        }
    }
}