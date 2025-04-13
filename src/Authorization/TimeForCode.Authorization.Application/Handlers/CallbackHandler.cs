using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class CallbackHandler : IRequestHandler<CallbackCommand, Result<TokenResult>>
    {
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<CallbackHandler> _logger;

        public CallbackHandler(IAccountService accountService,
            ITokenService tokenService,
            ILogger<CallbackHandler> logger)
        {
            _accountService = accountService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            var accessTokenResult = await _tokenService.GetAccessTokenFromExternalProviderAsync(request.State, request.Code);
            if (accessTokenResult.IsFailure)
            {
                return Result<TokenResult>.Failure(accessTokenResult.ErrorMessage);
            }

            _logger.LogDebug("Saving account information for state: {State}", request.State);
            var saveResult = await _accountService.SaveAccountInformation(request.State, accessTokenResult.Value);

            if (saveResult.IsFailure)
            {
                return Result<TokenResult>.Failure(saveResult.ErrorMessage);
            }

            var userId = saveResult.Value.Id.ToString();
            var internalToken = _tokenService.GenerateInternalToken(userId);
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(userId);
            var redirectUri = _tokenService.GetRedirectUri(request.State);

            return Result<TokenResult>.Success(new TokenResult
            {
                RedirectUri = redirectUri,
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