using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Application.Handlers.Admin
{
    public class CreateAdminRegistrationOptionsHandler : IRequestHandler<CreateAdminRegistrationOptionsCommand, Result<PasskeyCeremonyOptionsResult>>
    {
        private readonly IAdminCredentialRepository _adminCredentialRepository;
        private readonly IPasskeyCeremonyService _passkeyCeremonyService;
        private readonly AdminPasskeyOptions _options;
        private readonly ILogger<CreateAdminRegistrationOptionsHandler> _logger;

        public CreateAdminRegistrationOptionsHandler(IAdminCredentialRepository adminCredentialRepository,
            IPasskeyCeremonyService passkeyCeremonyService,
            IOptions<AdminPasskeyOptions> options,
            ILogger<CreateAdminRegistrationOptionsHandler> logger)
        {
            _adminCredentialRepository = adminCredentialRepository;
            _passkeyCeremonyService = passkeyCeremonyService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Result<PasskeyCeremonyOptionsResult>> Handle(CreateAdminRegistrationOptionsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling admin registration options request");

            if (!AdminBootstrapSecret.Matches(request.BootstrapSecret, _options.BootstrapSecret))
            {
                _logger.LogWarning("Admin registration options request rejected: invalid bootstrap secret");
                return Result<PasskeyCeremonyOptionsResult>.Failure("Invalid bootstrap secret.");
            }

            var existingCredential = await _adminCredentialRepository.GetAsync();
            if (existingCredential != null)
            {
                _logger.LogWarning("Admin registration options request rejected: admin role already claimed");
                return Result<PasskeyCeremonyOptionsResult>.Failure("The admin role has already been claimed.");
            }

            var options = await _passkeyCeremonyService.CreateRegistrationOptionsAsync();
            return Result<PasskeyCeremonyOptionsResult>.Success(options);
        }
    }

    /// <summary>
    /// Constant-time comparison for the admin bootstrap secret.
    /// </summary>
    internal static class AdminBootstrapSecret
    {
        public static bool Matches(string provided, string expected)
        {
            var providedBytes = Encoding.UTF8.GetBytes(provided);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);

            if (providedBytes.Length != expectedBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        }
    }
}