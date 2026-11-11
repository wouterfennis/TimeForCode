using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationHoursHandler : IRequestHandler<GetDonorOrganizationHoursQuery, Result<DonorOrganizationHoursResult>>
    {
        private readonly IDonorOrganizationRepository _organizationRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<GetDonorOrganizationHoursHandler> _logger;

        public GetDonorOrganizationHoursHandler(
            IDonorOrganizationRepository organizationRepository,
            IDonationRepository donationRepository,
            ILogger<GetDonorOrganizationHoursHandler> logger)
        {
            _organizationRepository = organizationRepository;
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<DonorOrganizationHoursResult>> Handle(GetDonorOrganizationHoursQuery request, CancellationToken cancellationToken)
        {
            var organization = await _organizationRepository.GetByIdAsync(request.DonorOrganizationId);
            if (organization == null)
            {
                _logger.LogWarning("Donor organization {DonorOrganizationId} not found", request.DonorOrganizationId);
                return Result<DonorOrganizationHoursResult>.NotFound("Donor organization not found.");
            }

            var donations = await _donationRepository.GetByDonorOrganizationIdsAsync([request.DonorOrganizationId], cancellationToken);
            var hoursLogged = donations
                .Where(d => d.Status is DonationStatus.Active or DonationStatus.Completed)
                .Sum(d => d.HoursLogged);

            return Result<DonorOrganizationHoursResult>.Success(new DonorOrganizationHoursResult { HoursLogged = hoursLogged });
        }
    }
}