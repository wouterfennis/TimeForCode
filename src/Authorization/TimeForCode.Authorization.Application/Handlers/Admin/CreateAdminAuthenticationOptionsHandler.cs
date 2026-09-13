using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Application.Handlers.Admin
{
    public class CreateAdminAuthenticationOptionsHandler : IRequestHandler<CreateAdminAuthenticationOptionsCommand, Result<PasskeyCeremonyOptionsResult>>
    {
        private readonly IAdminCredentialRepository _adminCredentialRepository;
        private readonly IPasskeyCeremonyService _passkeyCeremonyService;
        private readonly ILogger<CreateAdminAuthenticationOptionsHandler> _logger;

        public CreateAdminAuthenticationOptionsHandler(IAdminCredentialRepository adminCredentialRepository,
            IPasskeyCeremonyService passkeyCeremonyService,
            ILogger<CreateAdminAuthenticationOptionsHandler> logger)
        {
            _adminCredentialRepository = adminCredentialRepository;
            _passkeyCeremonyService = passkeyCeremonyService;
            _logger = logger;
        }

        public async Task<Result<PasskeyCeremonyOptionsResult>> Handle(CreateAdminAuthenticationOptionsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling admin authentication options request");

            var existingCredential = await _adminCredentialRepository.GetAsync();
            if (existingCredential == null)
            {
                _logger.LogWarning("Admin authentication options request rejected: no admin credential registered");
                return Result<PasskeyCeremonyOptionsResult>.Failure("No admin credential registered.");
            }

            var options = await _passkeyCeremonyService.CreateAuthenticationOptionsAsync();
            return Result<PasskeyCeremonyOptionsResult>.Success(options);
        }
    }
}