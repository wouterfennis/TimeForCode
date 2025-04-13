using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly RsaSecurityKey _rsaSecurityKey;
        private readonly IIdentityProviderServiceFactory _identityProviderServiceFactory;
        private readonly TimeProvider _timeProvider;
        private readonly IRandomGenerator _randomGenerator;
        private readonly AuthenticationOptions _authenticationOptions;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IStateRepository _stateRepository;
        private readonly ILogger<TokenService> _logger;

        public TokenService(RSA rsa,
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            TimeProvider timeProvider,
            IRandomGenerator randomGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IStateRepository stateRepository,
            IOptions<AuthenticationOptions> authenticationOptions,
            ILogger<TokenService> logger)
        {
            _rsaSecurityKey = new RsaSecurityKey(rsa);
            _authenticationOptions = authenticationOptions.Value;
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _timeProvider = timeProvider;
            _randomGenerator = randomGenerator;
            _refreshTokenRepository = refreshTokenRepository;
            _stateRepository = stateRepository;
            _logger = logger;
        }

        public async Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProviderAsync(string state, string code)
        {
            _logger.LogDebug("GetAccessTokenFromExternalProviderAsync called with state: {State}, code: {Code}", state, code);
            var result = _identityProviderServiceFactory.GetIdentityProviderServiceFromState(state);
            if (result.IsFailure)
            {
                return Result<ExternalAccessToken>.Failure(result.ErrorMessage);
            }
            var identityProviderService = result.Value;

            _logger.LogDebug("Getting access token from external provider with code: {Code}", code);
            var externalAccessTokenResult = await identityProviderService.GetAccessTokenAsync(code);
            if (externalAccessTokenResult.IsFailure)
            {
                return Result<ExternalAccessToken>.Failure(externalAccessTokenResult.ErrorMessage);
            }

            var accessToken = new ExternalAccessToken
            {
                Token = externalAccessTokenResult.Value.AccessToken,
            };
            return Result<ExternalAccessToken>.Success(accessToken);
        }

        public AccessToken GenerateInternalToken(string userId)
        {
            var claims = new Dictionary<string, object>
            {
                { "scope", "user" }
            };
            var expiresAfter = _timeProvider.GetUtcNow()
                .AddMinutes(_authenticationOptions.TokenExpiresInMinutes);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("sub", userId)]),
                Expires = expiresAfter.UtcDateTime,
                NotBefore = _timeProvider.GetUtcNow().UtcDateTime,
                Issuer = _authenticationOptions.Issuer,
                Audience = _authenticationOptions.Audience,
                Claims = claims,
                SigningCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new AccessToken
            {
                Token = tokenString,
                ExpiresAfter = expiresAfter
            };
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
        {
            var token = _randomGenerator.GenerateRandomString();
            var refreshToken = new Domain.Entities.RefreshToken
            {
                Id = ObjectId.GenerateNewId(),
                ExpiresAfter = _timeProvider.GetUtcNow().AddDays(_authenticationOptions.DefaultRefreshTokenExpirationAfterInDays),
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

        public async Task<Result<AccessToken>> RefreshInternalTokenAsync(RefreshToken refreshToken)
        {
            Domain.Entities.RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken.Token);

            if (existingToken == null)
            {
                return Result<AccessToken>.Failure("Refresh token not found");
            }

            if (existingToken.IsExpired(_timeProvider.GetUtcNow()))
            {
                return Result<AccessToken>.Failure("Refresh token expired");
            }

            return Result<AccessToken>.Success(GenerateInternalToken(existingToken.UserId));
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

        public Uri GetRedirectUri(string state)
        {
            var stateEntry = _stateRepository.GetState(state);
            return stateEntry == null ? throw new InvalidOperationException("State not found") : stateEntry.RedirectUri;
        }
    }
}