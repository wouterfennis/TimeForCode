using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly TimeProvider _timeProvider;
        private readonly IRandomGenerator _randomGenerator;
        private readonly TokenCreationOptions _tokenCreationOptions;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            TimeProvider timeProvider,
            IRandomGenerator randomGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<TokenCreationOptions> tokenCreationOptions,
            ILogger<RefreshTokenService> logger)
        {
            _tokenCreationOptions = tokenCreationOptions.Value;
            _timeProvider = timeProvider;
            _randomGenerator = randomGenerator;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
        {
            _logger.LogDebug("Creating refresh token for user {UserId}", userId);

            var token = _randomGenerator.GenerateRandomString();
            var refreshToken = Domain.Entities.RefreshToken.Issue(
                RefreshTokenHasher.Hash(token),
                userId,
                _timeProvider.GetUtcNow(),
                _tokenCreationOptions.DefaultRefreshTokenExpirationAfterInDays);

            await _refreshTokenRepository.CreateAsync(refreshToken);

            return new RefreshToken
            {
                Token = token,
                ExpiresAfter = refreshToken.ExpiresAfter,
            };
        }

        public async Task<Result<RefreshTokenReplacement>> ReplaceRefreshTokenAsync(RefreshToken oldRefreshToken)
        {
            Domain.Entities.RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenHashAsync(RefreshTokenHasher.Hash(oldRefreshToken.Token));

            if (existingToken == null)
            {
                _logger.LogWarning("Refresh token not found during replacement");
                return Result<RefreshTokenReplacement>.Failure("Refresh token not found");
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();

            var replacementCheck = existingToken.CheckReplaceable(now);

            if (replacementCheck == Domain.Entities.RefreshTokenReplacementCheck.Expired)
            {
                _logger.LogWarning("Refresh token expired for user {UserId}", existingToken.UserId);
                return Result<RefreshTokenReplacement>.Failure("Refresh token expired");
            }

            // The conditional update lets exactly one of two concurrent requests win the rotation.
            if (replacementCheck == Domain.Entities.RefreshTokenReplacementCheck.Replayed || !await _refreshTokenRepository.TryMarkRotatedAsync(existingToken.Id, now))
            {
                _logger.LogWarning("Rotated refresh token replayed for user {UserId}; revoking all refresh tokens", existingToken.UserId);
                await _refreshTokenRepository.DeleteByUserIdAsync(existingToken.UserId);
                return Result<RefreshTokenReplacement>.Failure("Refresh token reuse detected");
            }

            var newRefreshToken = await CreateRefreshTokenAsync(existingToken.UserId);

            _logger.LogDebug("Refresh token replaced for user {UserId}", existingToken.UserId);

            return Result<RefreshTokenReplacement>.Success(new RefreshTokenReplacement(newRefreshToken, existingToken.UserId));
        }

        public async Task ExpireRefreshTokenAsync(RefreshToken refreshToken)
        {
            Domain.Entities.RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenHashAsync(RefreshTokenHasher.Hash(refreshToken.Token));

            if (existingToken == null)
            {
                _logger.LogDebug("Refresh token not found during expiry; nothing to expire");
                return;
            }

            await ExpireRefreshTokenAsync(existingToken);
        }

        private async Task ExpireRefreshTokenAsync(Domain.Entities.RefreshToken refreshToken)
        {
            _logger.LogDebug("Expiring refresh token for user {UserId}", refreshToken.UserId);
            await _refreshTokenRepository.DeleteAsync(refreshToken);
        }
    }
}