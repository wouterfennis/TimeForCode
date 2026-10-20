using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationContributorsHandler : IRequestHandler<GetDonorOrganizationContributorsQuery, Result<GetDonorOrganizationContributorsResult>>
    {
        private readonly IContributorRepository _contributorRepository;
        private readonly IDonorOrganizationRepository _organizationRepository;
        private readonly ILogger<GetDonorOrganizationContributorsHandler> _logger;

        public GetDonorOrganizationContributorsHandler(
            IContributorRepository contributorRepository,
            IDonorOrganizationRepository organizationRepository,
            ILogger<GetDonorOrganizationContributorsHandler> logger)
        {
            _contributorRepository = contributorRepository;
            _organizationRepository = organizationRepository;
            _logger = logger;
        }

        public async Task<Result<GetDonorOrganizationContributorsResult>> Handle(GetDonorOrganizationContributorsQuery request, CancellationToken cancellationToken)
        {
            var organization = await _organizationRepository.GetByIdAsync(request.DonorOrganizationId);
            if (organization == null)
            {
                _logger.LogWarning("Donor organization {DonorOrganizationId} not found", request.DonorOrganizationId);
                return Result<GetDonorOrganizationContributorsResult>.NotFound("Donor organization not found.");
            }

            var contributors = await _contributorRepository.GetByDonorOrganizationIdAsync(ObjectId.Parse(request.DonorOrganizationId), cancellationToken);

            return Result<GetDonorOrganizationContributorsResult>.Success(new GetDonorOrganizationContributorsResult
            {
                Contributors = contributors.Select(c => c.ToProfileDto()).ToList()
            });
        }
    }
}