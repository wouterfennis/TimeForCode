using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Reporting.Application.Exports;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetContributionHistoryExportHandler : IRequestHandler<GetContributionHistoryExportQuery, Result<ContributionHistoryExportResult>>
    {
        private readonly IDonationDataSource _donationData;
        private readonly ILogger<GetContributionHistoryExportHandler> _logger;

        public GetContributionHistoryExportHandler(IDonationDataSource donationData, ILogger<GetContributionHistoryExportHandler> logger)
        {
            _donationData = donationData;
            _logger = logger;
        }

        public async Task<Result<ContributionHistoryExportResult>> Handle(GetContributionHistoryExportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Exporting the contribution history of donor organization {DonorOrganizationId} as {Format}", request.DonorOrganizationId, request.Format);

            var contributions = await _donationData.GetContributionsAsync(request.DonorOrganizationId, request.AccessToken, cancellationToken);
            if (contributions.IsFailure)
            {
                _logger.LogWarning("Contributions of donor organization {DonorOrganizationId} could not be read: {Reason}", request.DonorOrganizationId, contributions.ErrorMessage);
                return Result<ContributionHistoryExportResult>.Failure(contributions.ErrorMessage, contributions.FailureStatusCode);
            }

            FormattedExport export = ContributionHistoryFormatter.Format(contributions.Value, request.Format);

            return Result<ContributionHistoryExportResult>.Success(new ContributionHistoryExportResult
            {
                Content = export.Content,
                ContentType = export.ContentType,
                FileName = $"contribution-history-{request.DonorOrganizationId}.{export.FileExtension}"
            });
        }
    }
}