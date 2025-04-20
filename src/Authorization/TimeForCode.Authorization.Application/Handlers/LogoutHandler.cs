using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class LogoutHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<LogoutHandler> _logger;

        public LogoutHandler(IRefreshTokenService refreshTokenService,
            ILogger<LogoutHandler> logger)
        {
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (request.RefreshToken == null)
            {
                _logger.LogDebug("Refresh token is null during logout");
                return;
            }

            await _refreshTokenService.ExpireRefreshTokenAsync(request.RefreshToken);
        }
    }
}