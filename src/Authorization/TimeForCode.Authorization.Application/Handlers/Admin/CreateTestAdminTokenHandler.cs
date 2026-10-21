using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Application.Handlers.Admin
{
    public class CreateTestAdminTokenHandler : IRequestHandler<CreateTestAdminTokenCommand, Result<TokenResult>>
    {
        private const string Rejected = "Test admin access is not available.";

        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly TestAdminAccessOptions _options;
        private readonly ILogger<CreateTestAdminTokenHandler> _logger;

        public CreateTestAdminTokenHandler(ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            IOptions<TestAdminAccessOptions> options,
            ILogger<CreateTestAdminTokenHandler> logger)
        {
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(CreateTestAdminTokenCommand request, CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogWarning("Test admin token requested while test admin access is disabled");
                return Result<TokenResult>.Failure(Rejected);
            }

            if (!SecretMatches(request.Secret))
            {
                _logger.LogWarning("Test admin token requested with an incorrect secret");
                return Result<TokenResult>.Failure(Rejected);
            }

            _logger.LogWarning("Issuing a test-only admin token");

            var accessToken = _tokenService.GenerateInternalToken(AdminSession.UserId, AdminSession.Role, AdminSession.Scope);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(AdminSession.UserId);

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = accessToken,
                RefreshToken = refreshToken,
                IsNewUser = false
            });
        }

        private bool SecretMatches(string candidate)
        {
            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_options.Secret));
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(candidate));
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
    }
}