using MediatR;
using System.Net;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetOrganizationBadgeImageHandler : IRequestHandler<GetOrganizationBadgeImageQuery, Result<BadgeImageContent>>
    {
        private readonly IMediator _mediator;
        private readonly IBadgeTierRepository _tiers;

        public GetOrganizationBadgeImageHandler(IMediator mediator, IBadgeTierRepository tiers)
        {
            _mediator = mediator;
            _tiers = tiers;
        }

        public async Task<Result<BadgeImageContent>> Handle(GetOrganizationBadgeImageQuery request, CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<BadgeDto>> badges = await _mediator.Send(new GetOrganizationBadgesQuery { DonorOrganizationId = request.DonorOrganizationId }, cancellationToken);
            if (badges.IsFailure)
            {
                return Result<BadgeImageContent>.Success(badges.FailureStatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest
                    ? new BadgeImageContent(BadgeImageState.NotFound)
                    : new BadgeImageContent(BadgeImageState.Unavailable));
            }

            return Result<BadgeImageContent>.Success(HighestBadge.From(badges.Value, await _tiers.ListAsync(BadgeRecipientType.Organization, cancellationToken)));
        }
    }
}