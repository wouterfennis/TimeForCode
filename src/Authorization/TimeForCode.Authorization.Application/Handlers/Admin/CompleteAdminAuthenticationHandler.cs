using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Application.Handlers.Admin
{
    public class CompleteAdminAuthenticationHandler : IRequestHandler<CompleteAdminAuthenticationCommand, Result<TokenResult>>
    {
        private const string AdminUserId = "admin";

        private readonly IAdminCredentialRepository _adminCredentialRepository;
        private readonly IPasskeyCeremonyService _passkeyCeremonyService;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<CompleteAdminAuthenticationHandler> _logger;

        public CompleteAdminAuthenticationHandler(IAdminCredentialRepository adminCredentialRepository,
            IPasskeyCeremonyService passkeyCeremonyService,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            ILogger<CompleteAdminAuthenticationHandler> logger)
        {
            _adminCredentialRepository = adminCredentialRepository;
            _passkeyCeremonyService = passkeyCeremonyService;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(CompleteAdminAuthenticationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling admin authentication completion");

            var outcome = await _passkeyCeremonyService.CompleteAuthenticationAsync(request.CredentialJson, request.ProtectedCeremonyState);
            if (!outcome.Succeeded)
            {
                _logger.LogWarning("Admin authentication rejected: {Error}", outcome.ErrorMessage);
                return Result<TokenResult>.Failure("Admin authentication failed.");
            }

            await _adminCredentialRepository.UpdateSignCountAsync(outcome.SignCount);

            var accessToken = _tokenService.GenerateInternalToken(AdminUserId, "admin", "admin");
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(AdminUserId);

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = accessToken,
                RefreshToken = refreshToken,
                IsNewUser = false
            });
        }
    }
}