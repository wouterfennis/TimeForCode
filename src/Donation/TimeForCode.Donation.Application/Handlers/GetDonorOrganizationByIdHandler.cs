using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationByIdHandler : IRequestHandler<GetDonorOrganizationByIdQuery, Result<GetDonorOrganizationByIdResult>>
    {
        private readonly IDonorOrganizationRepository _repository;
        private readonly ILogger<GetDonorOrganizationByIdHandler> _logger;

        public GetDonorOrganizationByIdHandler(IDonorOrganizationRepository repository, ILogger<GetDonorOrganizationByIdHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<GetDonorOrganizationByIdResult>> Handle(GetDonorOrganizationByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting donor organization by id {Id}", request.Id);

            var organization = await _repository.GetByIdAsync(request.Id);
            if (organization == null)
            {
                _logger.LogWarning("Donor organization with id {Id} not found", request.Id);
                return Result<GetDonorOrganizationByIdResult>.Failure($"Donor organization with id '{request.Id}' was not found.");
            }

            return Result<GetDonorOrganizationByIdResult>.Success(new GetDonorOrganizationByIdResult
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