using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class CallbackHandler : IRequestHandler<CallbackCommand, Result<TokenResult>>
    {
        private readonly IStateRepository _stateRepository;
        private readonly TimeProvider _timeProvider;
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<CallbackHandler> _logger;

        public CallbackHandler(IStateRepository stateRepository,
            TimeProvider timeProvider,
            IAccountService accountService,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            ILogger<CallbackHandler> logger)
        {
            _stateRepository = stateRepository;
            _timeProvider = timeProvider;
            _accountService = accountService;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling callback");

            var stateEntry = await _stateRepository.TakeAsync(request.State);
            if (stateEntry == null)
            {
                _logger.LogWarning("Callback received with an unknown or already used state");
                return Result<TokenResult>.Failure("State is not known");
            }

            if (stateEntry.IsExpired(_timeProvider.GetUtcNow()))
            {
                _logger.LogWarning("Callback received with an expired state");
                return Result<TokenResult>.Failure("State has expired");
            }

            var accessTokenResult = await _tokenService.GetAccessTokenFromExternalProviderAsync(stateEntry, request.Code);
            if (accessTokenResult.IsFailure)
            {
                _logger.LogWarning("Failed to get access token from external provider: {Error}", accessTokenResult.ErrorMessage);
                return Result<TokenResult>.Failure(accessTokenResult.ErrorMessage);
            }

            var saveResult = await _accountService.SaveAccountInformation(stateEntry.IdentityProvider, accessTokenResult.Value);

            if (saveResult.IsFailure)
            {
                _logger.LogWarning("Failed to save account information: {Error}", saveResult.ErrorMessage);
                return Result<TokenResult>.Failure(saveResult.ErrorMessage);
            }

            var userId = saveResult.Value.AccountInformation.Id.ToString();
            var internalToken = _tokenService.GenerateInternalToken(saveResult.Value.AccountInformation);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(userId);

            _logger.LogInformation("Callback handled successfully for user {UserId}", userId);

            return Result<TokenResult>.Success(new TokenResult
            {
                RedirectUri = stateEntry.RedirectUri,
                IsNewUser = saveResult.Value.IsNewAccount,
                InternalAccessToken = new AccessToken
                {
                    Token = internalToken.Token,
                    ExpiresAfter = internalToken.ExpiresAfter
                },
                RefreshToken = new RefreshToken
                {
                    Token = refreshToken.Token,
                    ExpiresAfter = refreshToken.ExpiresAfter
                }
            });
        }
    }
}