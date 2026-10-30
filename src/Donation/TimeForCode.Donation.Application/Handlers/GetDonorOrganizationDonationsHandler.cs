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
            var denied = await DonorOrganizationAccess.CheckAsync<GetDonationsResult>(
                _organizationRepository, _contributorRepository, request.DonorOrganizationId, request.CallerUserId, request.IsAdministrator, _logger);
            if (denied != null)
            {
                return denied;
            }

            var donations = await _donationRepository.GetByDonorOrganizationIdsAsync([request.DonorOrganizationId], cancellationToken);

            return Result<GetDonationsResult>.Success(new GetDonationsResult
            {
                Donations = donations.Select(d => d.ToDto()).ToList()
            });
        }
    }
}