using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;
using TimeForCode.Shared.Identifiers;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetContributorBadgeImageHandler : IRequestHandler<GetContributorBadgeImageQuery, Result<BadgeImageContent>>
    {
        private readonly IMediator _mediator;
        private readonly IBadgeTierRepository _tiers;
        private readonly ILogger<GetContributorBadgeImageHandler> _logger;

        public GetContributorBadgeImageHandler(IMediator mediator, IBadgeTierRepository tiers, ILogger<GetContributorBadgeImageHandler> logger)
        {
            _mediator = mediator;
            _tiers = tiers;
            _logger = logger;
        }

        public Task<Result<BadgeImageContent>> Handle(GetContributorBadgeImageQuery request, CancellationToken cancellationToken)
        {
            // An unknown or malformed contributor looks exactly like one without badges, so the image does not confirm that an identifier exists.
            // The identifier is deliberately left out of the log message: an unavailable badge is a failure of ours or of Donation, not of the contributor.
            return BadgeImageFlow.RunAsync(
                ObjectIdFormat.IsValid(request.ContributorId),
                BadgeImageState.NoBadgeYet,
                () => _mediator.Send(new GetContributorBadgesQuery { ContributorId = request.ContributorId }, cancellationToken),
                async badges => HighestBadge.From(badges, await _tiers.ListAsync(BadgeRecipientType.Contributor, cancellationToken)),
                reason => _logger.LogWarning("The badge of a contributor is unavailable: {Reason}", reason));
        }
    }
}