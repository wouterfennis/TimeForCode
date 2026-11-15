using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    /// <summary>
    /// Exchanges a single-use login code for tokens. The code is consumed by the lookup, so a wrong verifier
    /// or a replay cannot be retried; the verifier must match the challenge sent at login.
    /// </summary>
    public class ExchangeCodeHandler : IRequestHandler<ExchangeCodeCommand, Result<TokenResult>>
    {
        private readonly ILoginCodeRepository _loginCodeRepository;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ExchangeCodeHandler> _logger;

        public ExchangeCodeHandler(ILoginCodeRepository loginCodeRepository,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            TimeProvider timeProvider,
            ILogger<ExchangeCodeHandler> logger)
        {
            _loginCodeRepository = loginCodeRepository;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(ExchangeCodeCommand request, CancellationToken cancellationToken)
        {
            var loginCode = await _loginCodeRepository.TakeAsync(RefreshTokenHasher.Hash(request.Code));
            if (loginCode == null)
            {
                _logger.LogWarning("Code exchange with an unknown or already used code");
                return Result<TokenResult>.Failure("Code is not valid");
            }

            if (loginCode.IsExpired(_timeProvider.GetUtcNow()))
            {
                _logger.LogWarning("Code exchange with an expired code");
                return Result<TokenResult>.Failure("Code is not valid");
            }

            var expectedChallenge = Encoding.ASCII.GetBytes(loginCode.CodeChallenge);
            var presentedChallenge = Encoding.ASCII.GetBytes(Pkce.CreateS256Challenge(request.CodeVerifier));
            if (!CryptographicOperations.FixedTimeEquals(expectedChallenge, presentedChallenge))
            {
                _logger.LogWarning("Code exchange with a verifier that does not match the login");
                return Result<TokenResult>.Failure("Code is not valid");
            }

            var accessToken = await _tokenService.GenerateInternalTokenForSessionAsync(loginCode.UserId);
            if (accessToken.IsFailure)
            {
                _logger.LogWarning("Failed to issue access token during code exchange: {Error}", accessToken.ErrorMessage);
                return Result<TokenResult>.Failure(accessToken.ErrorMessage);
            }

            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(loginCode.UserId);

            _logger.LogInformation("Code exchanged for user {UserId}", loginCode.UserId);

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = accessToken.Value,
                RefreshToken = refreshToken,
                IsNewUser = loginCode.IsNewUser
            });
        }
    }
}