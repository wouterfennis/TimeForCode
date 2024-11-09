using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class RefreshHandler : IRequestHandler<RefreshCommand, Result<TokenResult>>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<RefreshHandler> _logger;

        public RefreshHandler(IAccountService accountService,
            ITokenService tokenService,
            ILogger<RefreshHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
        {
            var internalTokenResult = await _tokenService.RefreshInternalTokenAsync(request.RefreshToken);

            if (internalTokenResult.IsFailure)
            {
                return Result<TokenResult>.Failure(internalTokenResult.ErrorMessage);
            }

            var refreshToken = await _tokenService.ReplaceRefreshTokenAsync(request.RefreshToken);

            if (refreshToken.IsFailure)
            {
                return Result<TokenResult>.Failure(refreshToken.ErrorMessage);
            }

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = internalTokenResult.Value,
                RefreshToken = refreshToken.Value
            });
        }
    }
}