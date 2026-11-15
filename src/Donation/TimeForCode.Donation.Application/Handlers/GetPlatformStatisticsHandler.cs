using MediatR;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetPlatformStatisticsHandler : IRequestHandler<GetPlatformStatisticsQuery, Result<PlatformStatisticsResult>>
    {
        private readonly IDonationRepository _donationRepository;

        public GetPlatformStatisticsHandler(IDonationRepository donationRepository)
        {
            _donationRepository = donationRepository;
        }

        public async Task<Result<PlatformStatisticsResult>> Handle(GetPlatformStatisticsQuery request, CancellationToken cancellationToken)
        {
            var (totalHours, organizations) = await _donationRepository.GetReceivedTotalsAsync(cancellationToken);
            return Result<PlatformStatisticsResult>.Success(new PlatformStatisticsResult { TotalHoursLogged = totalHours, OrganizationsSupported = organizations });
        }
    }
}