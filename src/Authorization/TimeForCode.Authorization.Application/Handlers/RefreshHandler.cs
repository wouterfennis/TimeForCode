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

            var replacement = await _refreshTokenService.ReplaceRefreshTokenAsync(request.RefreshToken);

            if (replacement.IsFailure)
            {
                _logger.LogWarning("Failed to replace refresh token: {Error}", replacement.ErrorMessage);
                return Result<TokenResult>.Failure(replacement.ErrorMessage);
            }

            _logger.LogInformation("Token refresh handled successfully");

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = _tokenService.GenerateInternalTokenForSession(replacement.Value.UserId),
                RefreshToken = replacement.Value.RefreshToken
            });
        }
    }
}