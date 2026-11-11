using MediatR;

namespace TimeForCode.Reporting.Commands
{
    public class GetOrganizationBadgesQuery : IRequest<Result<IReadOnlyList<BadgeDto>>>
    {
        public required string DonorOrganizationId { get; init; }
    }

    public class GetContributorBadgesQuery : IRequest<Result<IReadOnlyList<BadgeDto>>>
    {
        public required string ContributorId { get; init; }
    }

    public class GetPublicStatisticsQuery : IRequest<Result<PublicStatisticsResult>>
    {
    }

    /// <summary>A badge as shown publicly: its name and when it was awarded, nothing else.</summary>
    public class BadgeDto
    {
        public required string Name { get; init; }
        public required DateTimeOffset AwardedAt { get; init; }
    }

    public class PublicStatisticsResult
    {
        public required long TotalHoursLogged { get; init; }
        public required int OrganizationsSupported { get; init; }
    }
}