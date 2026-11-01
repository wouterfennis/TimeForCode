using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationDonationsHandler : IRequestHandler<GetDonorOrganizationDonationsQuery, Result<GetDonorOrganizationDonationsResult>>
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

        public async Task<Result<GetDonorOrganizationDonationsResult>> Handle(GetDonorOrganizationDonationsQuery request, CancellationToken cancellationToken)
        {
            var denied = await DonorOrganizationAccess.CheckAsync<GetDonorOrganizationDonationsResult>(
                _organizationRepository, _contributorRepository, request.DonorOrganizationId, request.CallerUserId, request.IsAdministrator, _logger);
            if (denied != null)
            {
                return denied;
            }

            var (donations, totalCount) = await _donationRepository.GetPageByDonorOrganizationIdAsync(
                request.DonorOrganizationId, request.PageNumber, request.PageSize, cancellationToken);

            return Result<GetDonorOrganizationDonationsResult>.Success(Page(donations.Select(d => d.ToDto()).ToList(), totalCount, request));
        }

        private static GetDonorOrganizationDonationsResult Page(IReadOnlyList<DonationDto> donations, int totalCount, GetDonorOrganizationDonationsQuery request)
        {
            return new GetDonorOrganizationDonationsResult
            {
                Donations = donations,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}