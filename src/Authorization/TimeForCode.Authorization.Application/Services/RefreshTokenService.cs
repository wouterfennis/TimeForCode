using Microsoft.Extensions.Options;
using MongoDB.Bson;
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

        public RefreshTokenService(
            TimeProvider timeProvider,
            IRandomGenerator randomGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<TokenCreationOptions> tokenCreationOptions)
        {
            _tokenCreationOptions = tokenCreationOptions.Value;
            _timeProvider = timeProvider;
            _randomGenerator = randomGenerator;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
        {
            var token = _randomGenerator.GenerateRandomString();
            var refreshToken = new Domain.Entities.RefreshToken
            {
                Id = ObjectId.GenerateNewId(),
                ExpiresAfter = _timeProvider.GetUtcNow().AddDays(_tokenCreationOptions.DefaultRefreshTokenExpirationAfterInDays),
                Token = token,
                UserId = userId
            };

            await _refreshTokenRepository.CreateAsync(refreshToken);

            return new RefreshToken
            {
                Token = token,
                ExpiresAfter = refreshToken.ExpiresAfter,
            };
        }

        public async Task<Result<RefreshToken>> ReplaceRefreshTokenAsync(RefreshToken oldRefreshToken)
        {
            Domain.Entities.RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenAsync(oldRefreshToken.Token);

            if (existingToken == null)
            {
                return Result<RefreshToken>.Failure("Refresh token not found");
            }

            await ExpireRefreshTokenAsync(existingToken);

            var newRefreshToken = await CreateRefreshTokenAsync(existingToken.UserId);
            return Result<RefreshToken>.Success(newRefreshToken);
        }

        public async Task ExpireRefreshTokenAsync(RefreshToken refreshToken)
        {
            Domain.Entities.RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken.Token);

            if (existingToken == null)
            {
                return;
            }

            await ExpireRefreshTokenAsync(existingToken);
        }

        private async Task ExpireRefreshTokenAsync(Domain.Entities.RefreshToken refreshToken)
        {
            refreshToken.SetExpiresAfter(_timeProvider.GetUtcNow());
            await _refreshTokenRepository.UpdateAsync(refreshToken);
        }
    }
}