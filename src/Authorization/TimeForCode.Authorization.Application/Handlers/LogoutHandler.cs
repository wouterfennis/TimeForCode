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

        public LogoutHandler(ITokenService tokenService,
            ILogger<LogoutHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (request.RefreshToken == null)
            {
                _logger.LogDebug("Refresh token is null during logout");
                return;
            }

            await _tokenService.ExpireRefreshTokenAsync(request.RefreshToken);
        }
    }
}