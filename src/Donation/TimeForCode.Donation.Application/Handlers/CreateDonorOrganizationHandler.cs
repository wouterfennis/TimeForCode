using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class CreateDonorOrganizationHandler : IRequestHandler<CreateDonorOrganizationCommand, Result<CreateDonorOrganizationResult>>
    {
        private readonly IDonorOrganizationRepository _repository;
        private readonly ILogger<CreateDonorOrganizationHandler> _logger;
        private readonly TimeProvider _timeProvider;

        public CreateDonorOrganizationHandler(IDonorOrganizationRepository repository, ILogger<CreateDonorOrganizationHandler> logger, TimeProvider timeProvider)
        {
            _repository = repository;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public async Task<Result<CreateDonorOrganizationResult>> Handle(CreateDonorOrganizationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating donor organization with name {Name}", request.Name);

            var existing = await _repository.GetByNameAsync(request.Name);
            if (existing != null)
            {
                _logger.LogWarning("Donor organization with name {Name} already exists", request.Name);
                return Result<CreateDonorOrganizationResult>.Conflict($"A donor organization with name '{request.Name}' already exists.");
            }

            var organization = DonorOrganization.Create(request.Name, request.ContactEmail, request.Website, _timeProvider.GetUtcNow());
            await _repository.CreateAsync(organization);

            _logger.LogInformation("Donor organization {Id} created successfully", organization.Id);

            return Result<CreateDonorOrganizationResult>.Success(new CreateDonorOrganizationResult
            {
                Organization = new DonorOrganizationDto
                {
                    Id = organization.Id.ToString(),
                    Name = organization.Name,
                    ContactEmail = organization.ContactEmail,
                    Website = organization.Website,
                    CreatedAt = organization.CreatedAt,
                    UpdatedAt = organization.UpdatedAt
                }
            });
        }
    }
}