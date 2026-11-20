using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GetOrganizationBadgeImageHandler> _logger;

        public GetOrganizationBadgeImageHandler(IMediator mediator, IBadgeTierRepository tiers, ILogger<GetOrganizationBadgeImageHandler> logger)
        {
            _mediator = mediator;
            _tiers = tiers;
            _logger = logger;
        }

        public async Task<Result<BadgeImageContent>> Handle(GetOrganizationBadgeImageQuery request, CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<BadgeDto>> badges = await _mediator.Send(new GetOrganizationBadgesQuery { DonorOrganizationId = request.DonorOrganizationId }, cancellationToken);
            if (badges.IsFailure)
            {
                if (badges.FailureStatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
                {
                    return Result<BadgeImageContent>.Success(new BadgeImageContent(BadgeImageState.NotFound));
                }

                _logger.LogWarning("The badge of donor organization {DonorOrganizationId} is unavailable: {Reason}", request.DonorOrganizationId, badges.ErrorMessage);
                return Result<BadgeImageContent>.Success(new BadgeImageContent(BadgeImageState.Unavailable));
            }

            return Result<BadgeImageContent>.Success(HighestBadge.From(badges.Value, await _tiers.ListAsync(BadgeRecipientType.Organization, cancellationToken)));
        }
    }
}