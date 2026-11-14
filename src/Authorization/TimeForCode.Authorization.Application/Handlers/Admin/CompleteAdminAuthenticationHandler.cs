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

            var credential = await _adminCredentialRepository.GetAsync();
            if (credential == null)
            {
                _logger.LogWarning("Admin authentication rejected: no admin credential registered");
                return Result<TokenResult>.Failure("Admin authentication failed.");
            }

            var outcome = await _passkeyCeremonyService.CompleteAuthenticationAsync(request.CredentialJson, request.CeremonyId);
            if (!outcome.Succeeded)
            {
                _logger.LogWarning("Admin authentication rejected: {Error}", outcome.ErrorMessage);
                return Result<TokenResult>.Failure("Admin authentication failed.");
            }

            if (!SignCountAdvanced(credential.SignCount, outcome.SignCount))
            {
                // A counter that did not increase means a cloned authenticator or a replayed assertion (WebAuthn level 3, section 7.2 step 22).
                _logger.LogWarning("Admin authentication rejected: the signature counter did not increase");
                return Result<TokenResult>.Failure("Admin authentication failed.");
            }

            if (!await _adminCredentialRepository.TryAdvanceSignCountAsync(outcome.SignCount))
            {
                // Another assertion advanced the counter in the meantime: this one is a replay or a clone.
                _logger.LogWarning("Admin authentication rejected: the signature counter was not advanced");
                return Result<TokenResult>.Failure("Admin authentication failed.");
            }

            var accessToken = _tokenService.GenerateInternalToken(AdminSession.UserId, AdminSession.Role, AdminSession.Scope);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(AdminSession.UserId);

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = accessToken,
                RefreshToken = refreshToken,
                IsNewUser = false
            });
        }

        // Authenticators that keep no counter (synced passkeys) always report 0, so 0 on both sides is fine; otherwise it must grow.
        private static bool SignCountAdvanced(long stored, long presented) =>
            (stored == 0 && presented == 0) || presented > stored;
    }
}