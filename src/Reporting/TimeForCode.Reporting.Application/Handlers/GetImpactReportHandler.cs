using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetImpactReportHandler : IRequestHandler<GetImpactReportQuery, Result<ImpactReportResult>>
    {
        private readonly IDonationDataSource _donationData;
        private readonly ILogger<GetImpactReportHandler> _logger;

        public GetImpactReportHandler(IDonationDataSource donationData, ILogger<GetImpactReportHandler> logger)
        {
            _donationData = donationData;
            _logger = logger;
        }

        public async Task<Result<ImpactReportResult>> Handle(GetImpactReportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Building the impact report of donor organization {DonorOrganizationId}", request.DonorOrganizationId);

            var donations = await _donationData.GetDonationsAsync(request.DonorOrganizationId, request.AccessToken, cancellationToken);
            if (donations.IsFailure)
            {
                _logger.LogWarning("Donations of donor organization {DonorOrganizationId} could not be read: {Reason}", request.DonorOrganizationId, donations.ErrorMessage);
                return Result<ImpactReportResult>.Failure(donations.ErrorMessage, donations.FailureStatusCode);
            }

            var report = ImpactReport.From(donations.Value);

            return Result<ImpactReportResult>.Success(new ImpactReportResult
            {
                HoursPledged = report.HoursPledged,
                HoursLogged = report.HoursLogged,
                ProjectsSupported = report.ProjectsSupported,
                Projects = report.Projects
                    .Select(p => new ProjectImpactDto { ProjectId = p.ProjectId, HoursPledged = p.HoursPledged, HoursLogged = p.HoursLogged })
                    .ToList()
            });
        }
    }
}