using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetMyDonationsHandler : IRequestHandler<GetMyDonationsQuery, Result<GetDonationsResult>>
    {
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<GetMyDonationsHandler> _logger;

        public GetMyDonationsHandler(IDonationRepository donationRepository, ILogger<GetMyDonationsHandler> logger)
        {
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<GetDonationsResult>> Handle(GetMyDonationsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting donations of user {UserId}", request.UserId);

            var donations = await _donationRepository.GetByPledgedByUserIdAsync(request.UserId, cancellationToken);

            return Result<GetDonationsResult>.Success(new GetDonationsResult
            {
                Donations = donations.Select(d => d.ToDto()).ToList()
            });
        }
    }
}