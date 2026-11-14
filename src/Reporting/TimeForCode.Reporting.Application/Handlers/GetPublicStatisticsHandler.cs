using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetPublicStatisticsHandler : IRequestHandler<GetPublicStatisticsQuery, Result<PublicStatisticsResult>>
    {
        private readonly IDonationDataSource _donationData;
        private readonly ILogger<GetPublicStatisticsHandler> _logger;

        public GetPublicStatisticsHandler(IDonationDataSource donationData, ILogger<GetPublicStatisticsHandler> logger)
        {
            _donationData = donationData;
            _logger = logger;
        }

        public async Task<Result<PublicStatisticsResult>> Handle(GetPublicStatisticsQuery request, CancellationToken cancellationToken)
        {
            Result<PlatformStatistics> statistics = await _donationData.GetPlatformStatisticsAsync(cancellationToken);
            if (statistics.IsFailure)
            {
                _logger.LogWarning("The platform statistics could not be read: {Reason}", statistics.ErrorMessage);
                return Result<PublicStatisticsResult>.Failure(statistics.ErrorMessage, statistics.FailureStatusCode);
            }

            return Result<PublicStatisticsResult>.Success(new PublicStatisticsResult
            {
                TotalHoursLogged = statistics.Value.TotalHoursLogged,
                OrganizationsSupported = statistics.Value.OrganizationsSupported
            });
        }
    }
}