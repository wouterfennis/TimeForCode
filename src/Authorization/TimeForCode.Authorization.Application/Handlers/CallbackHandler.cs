using MediatR;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class CallbackHandler : IRequestHandler<CallbackCommand, Result<TokenResult>>
    {
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;

        public CallbackHandler(IAccountService accountService,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService)
        {
            _accountService = accountService;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<Result<TokenResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            var accessTokenResult = await _tokenService.GetAccessTokenFromExternalProviderAsync(request.State, request.Code);
            if (accessTokenResult.IsFailure)
            {
                return Result<TokenResult>.Failure(accessTokenResult.ErrorMessage);
            }

            var saveResult = await _accountService.SaveAccountInformation(request.State, accessTokenResult.Value);

            if (saveResult.IsFailure)
            {
                return Result<TokenResult>.Failure(saveResult.ErrorMessage);
            }

            var userId = saveResult.Value.AccountInformation.Id.ToString();
            var internalToken = _tokenService.GenerateInternalToken(userId);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(userId);
            var redirectUri = _tokenService.GetRedirectUri(request.State);

            return Result<TokenResult>.Success(new TokenResult
            {
                RedirectUri = redirectUri,
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