using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Handlers.Admin
{
    public class CreateTestAdminTokenHandler : IRequestHandler<CreateTestAdminTokenCommand, Result<AccessToken>>
    {
        private const string Rejected = "Test admin access is not available.";

        private readonly ITokenService _tokenService;
        private readonly TestAdminAccessOptions _options;
        private readonly ILogger<CreateTestAdminTokenHandler> _logger;

        public CreateTestAdminTokenHandler(ITokenService tokenService,
            IOptions<TestAdminAccessOptions> options,
            ILogger<CreateTestAdminTokenHandler> logger)
        {
            _tokenService = tokenService;
            _options = options.Value;
            _logger = logger;
        }

        public Task<Result<AccessToken>> Handle(CreateTestAdminTokenCommand request, CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogWarning("Test admin token requested while test admin access is disabled");
                return Task.FromResult(Result<AccessToken>.Failure(Rejected));
            }

            if (!SecretMatches(request.Secret))
            {
                _logger.LogWarning("Test admin token requested with an incorrect secret");
                return Task.FromResult(Result<AccessToken>.Failure(Rejected));
            }

            _logger.LogWarning("Issuing a test-only admin token");

            return Task.FromResult(Result<AccessToken>.Success(_tokenService.GenerateTestAdminToken()));
        }

        private bool SecretMatches(string candidate)
        {
            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_options.Secret));
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(candidate));
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
    }
}