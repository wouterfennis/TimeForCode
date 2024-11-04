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

        public TokenService(RSA rsa,
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            TimeProvider timeProvider,
            IRandomGenerator randomGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<AuthenticationOptions> authenticationOptions)
        {
            _rsaSecurityKey = new RsaSecurityKey(rsa);
            _authenticationOptions = authenticationOptions.Value;
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _timeProvider = timeProvider;
            _randomGenerator = randomGenerator;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProvider(string state, string code)
        {
            var result = _identityProviderServiceFactory.GetIdentityProviderServiceFromState(state);
            if (result.IsFailure)
            {
                return Result<ExternalAccessToken>.Failure(result.ErrorMessage);
            }
            var identityProviderService = result.Value;

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
            var expiresAfter = _timeProvider.GetUtcNow().AddMinutes(_authenticationOptions.TokenExpiresInMinutes);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("sub", userId)]),
                Expires = _timeProvider.GetUtcNow().AddMinutes(_authenticationOptions.TokenExpiresInMinutes).UtcDateTime,
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

        public async Task<RefreshToken> CreateRefreshToken(string userId)
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

            return Result<AccessToken>.Success(GenerateInternalToken(existingToken.UserId));
        }

        public async Task<Result<RefreshToken>> ReplaceRefreshToken(RefreshToken oldRefreshToken)
        {
            Domain.Entities.RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenAsync(oldRefreshToken.Token);

            if (existingToken == null)
            {
                return Result<RefreshToken>.Failure("Refresh token not found");
            }

            existingToken.SetExpiresAfter(_timeProvider.GetUtcNow());
            await _refreshTokenRepository.UpdateAsync(existingToken);

            var newRefreshToken = await CreateRefreshToken(existingToken.UserId);
            return Result<RefreshToken>.Success(newRefreshToken);
        }
    }
}