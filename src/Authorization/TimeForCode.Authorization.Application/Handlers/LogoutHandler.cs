using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class LogoutHandler : IRequestHandler<LogoutCommand>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<LogoutHandler> _logger;

        public LogoutHandler(IAccountService accountService,
            ITokenService tokenService,
            ILogger<LogoutHandler> logger)
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

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (request.RefreshToken == null)
            {
                return;
            }

            await _tokenService.ExpireRefreshTokenAsync(request.RefreshToken);
        }
    }
}