using MediatR;

namespace TimeForCode.Reporting.Commands
{
    public class GetProjectDonationReportQuery : IRequest<Result<ProjectDonationReportResult>>
    {
        public required string ProjectId { get; init; }

        /// <summary>The caller's own access token; the Donation context decides with it whether the caller maintains the project. Never log or serialize it.</summary>
        public required string AccessToken { get; init; }

        /// <summary>An administrator may read the report of any project; everyone else only that of a project they maintain.</summary>
        public required bool IsAdministrator { get; init; }
    }
}