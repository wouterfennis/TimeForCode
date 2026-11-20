using MediatR;

namespace TimeForCode.Reporting.Commands
{
    /// <summary>What the badge image of a project shows.</summary>
    public enum ProjectBadgeKind
    {
        HoursReceived,
        Progress
    }

    /// <summary>What a badge image says: hours received, progress or a tier, or one of the neutral states that keep a README from showing a broken image.</summary>
    public enum BadgeImageState
    {
        HoursReceived,
        Progress,
        Tier,
        NoBadgeYet,
        NotFound,
        Unavailable
    }

    /// <summary>The content of a badge image: its state and, for the three value states, the text on the badge. Totals and tier names only.</summary>
    public record BadgeImageContent(BadgeImageState State, string? Text = null);

    public class GetProjectBadgeImageQuery : IRequest<Result<BadgeImageContent>>
    {
        public required string ProjectId { get; init; }
        public required ProjectBadgeKind Kind { get; init; }
    }

    public class GetOrganizationBadgeImageQuery : IRequest<Result<BadgeImageContent>>
    {
        public required string DonorOrganizationId { get; init; }
    }

    public class GetContributorBadgeImageQuery : IRequest<Result<BadgeImageContent>>
    {
        public required string ContributorId { get; init; }
    }
}