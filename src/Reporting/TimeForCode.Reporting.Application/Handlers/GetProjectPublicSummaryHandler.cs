using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetProjectPublicSummaryHandler : IRequestHandler<GetProjectPublicSummaryQuery, Result<ProjectPublicSummaryResult>>
    {
        private readonly IDonationDataSource _donationData;
        private readonly ILogger<GetProjectPublicSummaryHandler> _logger;

        public GetProjectPublicSummaryHandler(IDonationDataSource donationData, ILogger<GetProjectPublicSummaryHandler> logger)
        {
            _donationData = donationData;
            _logger = logger;
        }

        public async Task<Result<ProjectPublicSummaryResult>> Handle(GetProjectPublicSummaryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Building the public summary of project {ProjectId}", request.ProjectId);

            Result<IReadOnlyList<ProjectDonation>> donations = await _donationData.GetProjectDonationsAsync(request.ProjectId, cancellationToken);
            if (donations.IsFailure)
            {
                _logger.LogWarning("Donations of project {ProjectId} could not be read: {Reason}", request.ProjectId, donations.ErrorMessage);
                return Result<ProjectPublicSummaryResult>.Failure(donations.ErrorMessage, donations.FailureStatusCode);
            }

            Result<IReadOnlyList<ProjectContribution>> contributions = await _donationData.GetProjectContributionsAsync(request.ProjectId, cancellationToken);
            if (contributions.IsFailure)
            {
                _logger.LogWarning("Contributions of project {ProjectId} could not be read: {Reason}", request.ProjectId, contributions.ErrorMessage);
                return Result<ProjectPublicSummaryResult>.Failure(contributions.ErrorMessage, contributions.FailureStatusCode);
            }

            ProjectDonationReport report = ProjectDonationReport.From(donations.Value, contributions.Value);

            return Result<ProjectPublicSummaryResult>.Success(new ProjectPublicSummaryResult
            {
                HoursReceived = report.HoursReceived,
                HoursLogged = report.HoursLogged,
                ProgressPercentage = report.ProgressPercentage,
                DonorCount = report.DonorCount,
                ContributorCount = report.ContributorCount
            });
        }
    }
}