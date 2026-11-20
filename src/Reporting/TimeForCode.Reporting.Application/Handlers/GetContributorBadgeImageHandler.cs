using MediatR;
using System.Net;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetContributorBadgeImageHandler : IRequestHandler<GetContributorBadgeImageQuery, Result<BadgeImageContent>>
    {
        private readonly IMediator _mediator;
        private readonly IBadgeTierRepository _tiers;

        public GetContributorBadgeImageHandler(IMediator mediator, IBadgeTierRepository tiers)
        {
            _mediator = mediator;
            _tiers = tiers;
        }

        public async Task<Result<BadgeImageContent>> Handle(GetContributorBadgeImageQuery request, CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<BadgeDto>> badges = await _mediator.Send(new GetContributorBadgesQuery { ContributorId = request.ContributorId }, cancellationToken);
            if (badges.IsFailure)
            {
                // An unknown contributor looks exactly like one without badges, so the image does not confirm that an identifier exists.
                return Result<BadgeImageContent>.Success(badges.FailureStatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest
                    ? new BadgeImageContent(BadgeImageState.NoBadgeYet)
                    : new BadgeImageContent(BadgeImageState.Unavailable));
            }

            return Result<BadgeImageContent>.Success(HighestBadge.From(badges.Value, await _tiers.ListAsync(BadgeRecipientType.Contributor, cancellationToken)));
        }
    }
}