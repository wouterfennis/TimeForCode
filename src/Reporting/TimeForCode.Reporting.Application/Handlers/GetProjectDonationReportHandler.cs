using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetProjectDonationReportHandler : IRequestHandler<GetProjectDonationReportQuery, Result<ProjectDonationReportResult>>
    {
        private readonly IDonationDataSource _donationData;
        private readonly ILogger<GetProjectDonationReportHandler> _logger;

        public GetProjectDonationReportHandler(IDonationDataSource donationData, ILogger<GetProjectDonationReportHandler> logger)
        {
            _donationData = donationData;
            _logger = logger;
        }

        public async Task<Result<ProjectDonationReportResult>> Handle(GetProjectDonationReportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Building the donation report of project {ProjectId}", request.ProjectId);

            if (!request.IsAdministrator)
            {
                Result<bool> maintainer = await _donationData.VerifyMaintainerAsync(request.ProjectId, request.AccessToken, cancellationToken);
                if (maintainer.IsFailure)
                {
                    _logger.LogWarning("Project {ProjectId} is not readable for the caller: {Reason}", request.ProjectId, maintainer.ErrorMessage);
                    return Result<ProjectDonationReportResult>.Failure(maintainer.ErrorMessage, maintainer.FailureStatusCode);
                }
            }

            Result<IReadOnlyList<ProjectDonation>> donations = await _donationData.GetProjectDonationsAsync(request.ProjectId, cancellationToken);
            if (donations.IsFailure)
            {
                _logger.LogWarning("Donations of project {ProjectId} could not be read: {Reason}", request.ProjectId, donations.ErrorMessage);
                return Result<ProjectDonationReportResult>.Failure(donations.ErrorMessage, donations.FailureStatusCode);
            }

            Result<IReadOnlyList<ProjectContribution>> contributions = await _donationData.GetProjectContributionsAsync(request.ProjectId, cancellationToken);
            if (contributions.IsFailure)
            {
                _logger.LogWarning("Contributions of project {ProjectId} could not be read: {Reason}", request.ProjectId, contributions.ErrorMessage);
                return Result<ProjectDonationReportResult>.Failure(contributions.ErrorMessage, contributions.FailureStatusCode);
            }

            ProjectDonationReport report = ProjectDonationReport.From(donations.Value, contributions.Value);

            return Result<ProjectDonationReportResult>.Success(new ProjectDonationReportResult
            {
                HoursReceived = report.HoursReceived,
                HoursLogged = report.HoursLogged,
                ProgressPercentage = report.ProgressPercentage,
                DonorCount = report.DonorCount,
                ContributorCount = report.ContributorCount,
                PendingDonations = report.PendingDonations,
                ActiveDonations = report.ActiveDonations,
                CompletedDonations = report.CompletedDonations,
                CancelledDonations = report.CancelledDonations,
                Donations = report.Donations
                    .Select(d => new ProjectDonationDto
                    {
                        Id = d.Id,
                        DonorOrganizationId = d.DonorOrganizationId,
                        Status = d.Status,
                        HoursCommitted = d.HoursCommitted,
                        HoursLogged = d.HoursLogged,
                        CreatedAt = d.CreatedAt
                    })
                    .ToList(),
                Contributors = report.Contributors
                    .Select(c => new ProjectContributorDto { ContributorId = c.ContributorId, ContributorLogin = c.ContributorLogin, Hours = c.Hours })
                    .ToList()
            });
        }
    }
}