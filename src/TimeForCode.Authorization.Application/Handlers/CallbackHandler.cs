using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class CallbackHandler : IRequestHandler<CallbackCommand, Result<CallbackResult>>
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

        public async Task<Result<CallbackResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            var accessTokenResult = await _tokenService.GetAccessTokenFromExternalProvider(request.State, request.Code);
            if (accessTokenResult.IsFailure)
            {
                return Result<CallbackResult>.Failure(accessTokenResult.ErrorMessage);
            }

            var saveResult = await _accountService.SaveAccountInformation(request.State, accessTokenResult.Value);

            if (saveResult.IsFailure)
            {
                return Result<CallbackResult>.Failure(saveResult.ErrorMessage);
            }

            var internalToken = _tokenService.GenerateInternalToken(saveResult.Value.Id.ToString());
            var refreshToken = await _tokenService.CreateAndReplaceRefreshToken(null);

            return Result<CallbackResult>.Success(new CallbackResult
            {
                InternalAccessToken = new AccessToken
                {
                    Token = internalToken.Token
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