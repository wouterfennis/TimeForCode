using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetOrganizationBadgesHandler : IRequestHandler<GetOrganizationBadgesQuery, Result<IReadOnlyList<BadgeDto>>>
    {
        private readonly IDonationDataSource _donationData;
        private readonly BadgeAwarder _awarder;
        private readonly ILogger<GetOrganizationBadgesHandler> _logger;

        public GetOrganizationBadgesHandler(IDonationDataSource donationData, BadgeAwarder awarder, ILogger<GetOrganizationBadgesHandler> logger)
        {
            _donationData = donationData;
            _awarder = awarder;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<BadgeDto>>> Handle(GetOrganizationBadgesQuery request, CancellationToken cancellationToken)
        {
            Result<int> hours = await _donationData.GetOrganizationHoursAsync(request.DonorOrganizationId, cancellationToken);
            if (hours.IsFailure)
            {
                _logger.LogWarning("Hours of donor organization {DonorOrganizationId} could not be read: {Reason}", request.DonorOrganizationId, hours.ErrorMessage);
                return Result<IReadOnlyList<BadgeDto>>.Failure(hours.ErrorMessage, hours.FailureStatusCode);
            }

            return Result<IReadOnlyList<BadgeDto>>.Success(
                await _awarder.AwardAsync(BadgeRecipientType.Organization, request.DonorOrganizationId, hours.Value, cancellationToken));
        }
    }
}