using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Infrastructure.Options;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class DonationService : IDonationService
    {
        private const string ServiceUserId = "authorization-service";
        private const string ServiceRole = "service";
        private const string ServiceScope = "service";

        private readonly DonationOptions _options;
        private readonly RestClient _restClient;
        private readonly ITokenService _tokenService;
        private readonly ILogger<DonationService> _logger;

        public DonationService(
            IOptions<DonationOptions> options,
            RestClient restClient,
            ITokenService tokenService,
            ILogger<DonationService> logger)
        {
            _options = options.Value;
            _restClient = restClient;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<Unit>> AnonymiseContributorAsync(string userId)
        {
            if (!Uri.TryCreate(_options.BaseUri, UriKind.Absolute, out var baseUri))
            {
                _logger.LogError("DonationOptions:BaseUri is not configured or not an absolute URI");
                return Result<Unit>.Failure("The donation API address is not configured");
            }

            var token = _tokenService.GenerateInternalToken(ServiceUserId, ServiceRole, ServiceScope);
            var uri = new Uri(baseUri, $"api/v1/contributor/{Uri.EscapeDataString(userId)}/anonymise");

            var request = new RestRequest(uri.ToString(), Method.Post);
            request.AddHeader("Authorization", $"Bearer {token.Token}");

            var response = await _restClient.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            _logger.LogWarning("Donation API refused to anonymise the contributor: {StatusCode} {Error}", response.StatusCode, response.ErrorMessage);
            return Result<Unit>.Failure($"Donation API responded with {(int)response.StatusCode} {response.ErrorMessage}".Trim());
        }
    }
}