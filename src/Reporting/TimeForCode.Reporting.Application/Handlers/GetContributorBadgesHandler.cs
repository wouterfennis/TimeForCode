using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetContributorBadgesHandler : IRequestHandler<GetContributorBadgesQuery, Result<IReadOnlyList<BadgeDto>>>
    {
        private readonly IDonationDataSource _donationData;
        private readonly BadgeAwarder _awarder;
        private readonly ILogger<GetContributorBadgesHandler> _logger;

        public GetContributorBadgesHandler(IDonationDataSource donationData, BadgeAwarder awarder, ILogger<GetContributorBadgesHandler> logger)
        {
            _donationData = donationData;
            _awarder = awarder;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<BadgeDto>>> Handle(GetContributorBadgesQuery request, CancellationToken cancellationToken)
        {
            Result<int> hours = await _donationData.GetContributorHoursAsync(request.ContributorId, cancellationToken);
            if (hours.IsFailure)
            {
                _logger.LogWarning("Hours of contributor {ContributorId} could not be read: {Reason}", request.ContributorId, hours.ErrorMessage);
                return Result<IReadOnlyList<BadgeDto>>.Failure(hours.ErrorMessage, hours.FailureStatusCode);
            }

            return Result<IReadOnlyList<BadgeDto>>.Success(
                await _awarder.AwardAsync(BadgeRecipientType.Contributor, request.ContributorId, hours.Value, cancellationToken));
        }
    }
}