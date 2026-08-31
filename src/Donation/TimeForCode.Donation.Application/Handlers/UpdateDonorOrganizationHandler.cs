using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class UpdateDonorOrganizationHandler : IRequestHandler<UpdateDonorOrganizationCommand, Result<UpdateDonorOrganizationResult>>
    {
        private readonly IDonorOrganizationRepository _repository;
        private readonly ILogger<UpdateDonorOrganizationHandler> _logger;

        public UpdateDonorOrganizationHandler(IDonorOrganizationRepository repository, ILogger<UpdateDonorOrganizationHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<UpdateDonorOrganizationResult>> Handle(UpdateDonorOrganizationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating donor organization {Id}", request.Id);

            var organization = await _repository.GetByIdAsync(request.Id);
            if (organization == null)
            {
                _logger.LogWarning("Donor organization with id {Id} not found", request.Id);
                return Result<UpdateDonorOrganizationResult>.Failure($"Donor organization with id '{request.Id}' was not found.");
            }

            if (!string.Equals(organization.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var existingWithName = await _repository.GetByNameAsync(request.Name);
                if (existingWithName != null)
                {
                    _logger.LogWarning("Donor organization with name {Name} already exists", request.Name);
                    return Result<UpdateDonorOrganizationResult>.Conflict($"A donor organization with name '{request.Name}' already exists.");
                }
            }

            organization.Name = request.Name;
            organization.ContactEmail = request.ContactEmail;
            organization.Website = request.Website;
            organization.UpdatedAt = DateTimeOffset.UtcNow;

            await _repository.UpdateAsync(organization);

            _logger.LogInformation("Donor organization {Id} updated successfully", organization.Id);

            return Result<UpdateDonorOrganizationResult>.Success(new UpdateDonorOrganizationResult
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