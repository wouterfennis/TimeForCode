using MediatR;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Commands
{
    public class GetContributionHistoryExportQuery : IRequest<Result<ContributionHistoryExportResult>>
    {
        public required string DonorOrganizationId { get; init; }

        /// <summary>The caller's own access token; the Donation context decides with it whether the caller may read the organization.</summary>
        public required string AccessToken { get; init; }

        public required ExportFormat Format { get; init; }
    }
}