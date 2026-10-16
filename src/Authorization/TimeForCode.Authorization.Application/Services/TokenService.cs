using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;
using AccountInformation = TimeForCode.Authorization.Domain.Entities.AccountInformation;

namespace TimeForCode.Authorization.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly SigningKeySet _signingKeys;
        private readonly IIdentityProviderServiceFactory _identityProviderServiceFactory;
        private readonly TimeProvider _timeProvider;
        private readonly TokenCreationOptions _tokenCreationOptions;
        private readonly IAccountInformationRepository _accountInformationRepository;
        private readonly ILogger<TokenService> _logger;

        public TokenService(SigningKeySet signingKeys,
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            TimeProvider timeProvider,
            IAccountInformationRepository accountInformationRepository,
            IOptions<TokenCreationOptions> tokenCreationOptions,
            ILogger<TokenService> logger)
        {
            _signingKeys = signingKeys;
            _tokenCreationOptions = tokenCreationOptions.Value;
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _timeProvider = timeProvider;
            _accountInformationRepository = accountInformationRepository;
            _logger = logger;
        }

        public async Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProviderAsync(StateEntry stateEntry, string code)
        {
            _logger.LogDebug("Getting access token from external provider {IdentityProvider}", stateEntry.IdentityProvider);

            var result = _identityProviderServiceFactory.GetIdentityProviderService(stateEntry.IdentityProvider);
            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to get identity provider service: {Error}", result.ErrorMessage);
                return Result<ExternalAccessToken>.Failure(result.ErrorMessage);
            }
            var identityProviderService = result.Value;

            var externalAccessTokenResult = await identityProviderService.GetAccessTokenAsync(code, stateEntry.CodeVerifier);
            if (externalAccessTokenResult.IsFailure)
            {
                _logger.LogWarning("Failed to get access token from identity provider: {Error}", externalAccessTokenResult.ErrorMessage);
                return Result<ExternalAccessToken>.Failure(externalAccessTokenResult.ErrorMessage);
            }

            var accessToken = new ExternalAccessToken
            {
                Token = externalAccessTokenResult.Value.AccessToken,
            };
            return Result<ExternalAccessToken>.Success(accessToken);
        }

        public AccessToken GenerateInternalToken(AccountInformation account)
        {
            return BuildInternalToken(
                account.Id.ToString(),
                role: null,
                scope: "user",
                login: account.Login,
                avatarUrl: account.AvatarUrl);
        }

        public AccessToken GenerateInternalToken(string userId, string role, string scope)
        {
            return BuildInternalToken(userId, role, scope);
        }

        private AccessToken BuildInternalToken(string userId, string? role, string scope, string? login = null, string? avatarUrl = null)
        {
            _logger.LogDebug("Generating internal token for user {UserId}", userId);

            var expiresAfter = _timeProvider.GetUtcNow()
                .AddMinutes(_tokenCreationOptions.TokenExpiresInMinutes);

            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new Dictionary<string, object>
            {
                { "sub", userId },
                { "scope", scope },
                { "aud", _tokenCreationOptions.Audiences }
            };

            if (role != null)
            {
                claims["role"] = role;
            }

            if (login != null)
            {
                claims["login"] = login;
            }

            if (avatarUrl != null)
            {
                claims["avatar_url"] = avatarUrl;
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = expiresAfter.UtcDateTime,
                NotBefore = _timeProvider.GetUtcNow().UtcDateTime,
                Issuer = _tokenCreationOptions.Issuer,
                Claims = claims,
                SigningCredentials = new SigningCredentials(_signingKeys.Current, SigningKeySet.Algorithm)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new AccessToken
            {
                Token = tokenString,
                ExpiresAfter = expiresAfter
            };
        }

        public async Task<Result<AccessToken>> GenerateInternalTokenForSessionAsync(string userId)
        {
            if (userId == AdminSession.UserId)
            {
                return Result<AccessToken>.Success(GenerateInternalToken(AdminSession.UserId, AdminSession.Role, AdminSession.Scope));
            }

            var account = await _accountInformationRepository.GetByInternalIdAsync(userId);
            if (account == null)
            {
                _logger.LogWarning("Account {UserId} not found during refresh", userId);
                return Result<AccessToken>.Failure("Account not found");
            }

            return Result<AccessToken>.Success(GenerateInternalToken(account));
        }
    }
}