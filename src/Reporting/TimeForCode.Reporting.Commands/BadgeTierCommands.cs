using MediatR;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Commands
{
    public class DefineBadgeTierCommand : IRequest<Result<BadgeTierDto>>
    {
        public required BadgeRecipientType RecipientType { get; init; }
        public required string Name { get; init; }
        public required int ThresholdHours { get; init; }
    }

    public class RemoveBadgeTierCommand : IRequest<Result<bool>>
    {
        public required BadgeRecipientType RecipientType { get; init; }
        public required string Name { get; init; }
    }

    public class GetBadgeTiersQuery : IRequest<Result<IReadOnlyList<BadgeTierDto>>>
    {
        public BadgeRecipientType? RecipientType { get; init; }
    }

    public class BadgeTierDto
    {
        public required string Name { get; init; }
        public required BadgeRecipientType RecipientType { get; init; }
        public required int ThresholdHours { get; init; }
    }
}