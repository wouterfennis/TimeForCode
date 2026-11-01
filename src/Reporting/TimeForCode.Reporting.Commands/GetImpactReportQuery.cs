using MediatR;

namespace TimeForCode.Reporting.Commands
{
    public class GetImpactReportQuery : IRequest<Result<ImpactReportResult>>
    {
        public required string DonorOrganizationId { get; init; }

        /// <summary>The caller's own access token; the Donation context decides with it whether the caller may read the organization. Never log or serialize it.</summary>
        public required string AccessToken { get; init; }
    }
}