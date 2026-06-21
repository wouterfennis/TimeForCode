using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class RefreshHandler : IRequestHandler<RefreshCommand, Result<TokenResult>>
    {
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<RefreshHandler> _logger;

        public RefreshHandler(ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            ILogger<RefreshHandler> logger)
        {
            _refreshTokenService = refreshTokenService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling token refresh");

            var internalTokenResult = await _tokenService.RefreshInternalTokenAsync(request.RefreshToken);

            if (internalTokenResult.IsFailure)
            {
                _logger.LogWarning("Failed to refresh internal token: {Error}", internalTokenResult.ErrorMessage);
                return Result<TokenResult>.Failure(internalTokenResult.ErrorMessage);
            }

            var refreshToken = await _refreshTokenService.ReplaceRefreshTokenAsync(request.RefreshToken);

            if (refreshToken.IsFailure)
            {
                _logger.LogWarning("Failed to replace refresh token: {Error}", refreshToken.ErrorMessage);
                return Result<TokenResult>.Failure(refreshToken.ErrorMessage);
            }

            _logger.LogInformation("Token refresh handled successfully");

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = internalTokenResult.Value,
                RefreshToken = refreshToken.Value
            });
        }
    }
}