using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationDonationsHandler : IRequestHandler<GetDonorOrganizationDonationsQuery, Result<GetDonationsResult>>
    {
        private readonly IDonorOrganizationRepository _organizationRepository;
        private readonly IContributorRepository _contributorRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<GetDonorOrganizationDonationsHandler> _logger;

        public GetDonorOrganizationDonationsHandler(
            IDonorOrganizationRepository organizationRepository,
            IContributorRepository contributorRepository,
            IDonationRepository donationRepository,
            ILogger<GetDonorOrganizationDonationsHandler> logger)
        {
            _organizationRepository = organizationRepository;
            _contributorRepository = contributorRepository;
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<GetDonationsResult>> Handle(GetDonorOrganizationDonationsQuery request, CancellationToken cancellationToken)
        {
            var organization = await _organizationRepository.GetByIdAsync(request.DonorOrganizationId);
            if (organization == null)
            {
                _logger.LogWarning("Donor organization {DonorOrganizationId} not found", request.DonorOrganizationId);
                return Result<GetDonationsResult>.NotFound("Donor organization not found.");
            }

            if (!request.IsAdministrator)
            {
                var caller = await _contributorRepository.GetByPlatformUserIdAsync(request.CallerUserId);
                if (caller?.DonorOrganizationId?.ToString() != request.DonorOrganizationId)
                {
                    _logger.LogWarning("User {UserId} is not a member of donor organization {DonorOrganizationId}", request.CallerUserId, request.DonorOrganizationId);
                    return Result<GetDonationsResult>.Forbidden("Only members of the donor organization or an administrator can access its donations.");
                }
            }

            var donations = await _donationRepository.GetByDonorOrganizationIdsAsync([request.DonorOrganizationId], cancellationToken);

            return Result<GetDonationsResult>.Success(new GetDonationsResult
            {
                Donations = donations.Select(d => d.ToDto()).ToList()
            });
        }
    }
}