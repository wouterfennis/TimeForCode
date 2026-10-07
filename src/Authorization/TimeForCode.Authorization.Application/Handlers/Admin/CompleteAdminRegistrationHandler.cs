using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Handlers.Admin
{
    public class CompleteAdminRegistrationHandler : IRequestHandler<CompleteAdminRegistrationCommand, Result<TokenResult>>
    {
        private const string AdminUserId = "admin";

        private readonly IAdminCredentialRepository _adminCredentialRepository;
        private readonly IPasskeyCeremonyService _passkeyCeremonyService;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly TimeProvider _timeProvider;
        private readonly AdminPasskeyOptions _options;
        private readonly ILogger<CompleteAdminRegistrationHandler> _logger;

        public CompleteAdminRegistrationHandler(IAdminCredentialRepository adminCredentialRepository,
            IPasskeyCeremonyService passkeyCeremonyService,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            TimeProvider timeProvider,
            IOptions<AdminPasskeyOptions> options,
            ILogger<CompleteAdminRegistrationHandler> logger)
        {
            _adminCredentialRepository = adminCredentialRepository;
            _passkeyCeremonyService = passkeyCeremonyService;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _timeProvider = timeProvider;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Result<TokenResult>> Handle(CompleteAdminRegistrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling admin registration completion");

            if (!AdminBootstrapSecret.Matches(request.BootstrapSecret, _options.BootstrapSecret))
            {
                _logger.LogWarning("Admin registration completion rejected: invalid bootstrap secret");
                return Result<TokenResult>.Failure("Invalid bootstrap secret.");
            }

            var outcome = await _passkeyCeremonyService.CompleteRegistrationAsync(request.CredentialJson, request.ProtectedCeremonyState);
            if (!outcome.Succeeded)
            {
                _logger.LogWarning("Admin registration completion rejected: {Error}", outcome.ErrorMessage);
                return Result<TokenResult>.Failure("Admin passkey registration failed.");
            }

            var credential = new AdminCredential
            {
                Id = AdminCredential.SingletonId,
                CredentialId = outcome.CredentialId!,
                PublicKey = outcome.PublicKey!,
                SignCount = outcome.SignCount,
                Transports = outcome.Transports ?? [],
                IsUserVerified = outcome.IsUserVerified,
                IsBackupEligible = outcome.IsBackupEligible,
                IsBackedUp = outcome.IsBackedUp,
                CreatedAt = _timeProvider.GetUtcNow()
            };

            var claimed = await _adminCredentialRepository.TryClaimAsync(credential);
            if (!claimed)
            {
                _logger.LogWarning("Admin registration completion rejected: admin role already claimed by another device");
                return Result<TokenResult>.Failure("The admin role has already been claimed.");
            }

            _logger.LogInformation("Admin role permanently claimed");

            return await IssueTokensAsync();
        }

        private async Task<Result<TokenResult>> IssueTokensAsync()
        {
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