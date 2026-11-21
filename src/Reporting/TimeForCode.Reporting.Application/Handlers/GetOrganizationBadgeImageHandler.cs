using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;
using TimeForCode.Shared.Identifiers;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetOrganizationBadgeImageHandler : IRequestHandler<GetOrganizationBadgeImageQuery, Result<BadgeImageContent>>
    {
        private readonly IMediator _mediator;
        private readonly IBadgeTierRepository _tiers;
        private readonly ILogger<GetOrganizationBadgeImageHandler> _logger;

        public GetOrganizationBadgeImageHandler(IMediator mediator, IBadgeTierRepository tiers, ILogger<GetOrganizationBadgeImageHandler> logger)
        {
            _mediator = mediator;
            _tiers = tiers;
            _logger = logger;
        }

        public Task<Result<BadgeImageContent>> Handle(GetOrganizationBadgeImageQuery request, CancellationToken cancellationToken)
        {
            return BadgeImageFlow.RunAsync(
                ObjectIdFormat.IsValid(request.DonorOrganizationId),
                BadgeImageState.NotFound,
                () => _mediator.Send(new GetOrganizationBadgesQuery { DonorOrganizationId = request.DonorOrganizationId }, cancellationToken),
                async badges => HighestBadge.From(badges, await _tiers.ListAsync(BadgeRecipientType.Organization, cancellationToken)),
                reason => _logger.LogWarning("The badge of donor organization {DonorOrganizationId} is unavailable: {Reason}", request.DonorOrganizationId, reason));
        }
    }
}