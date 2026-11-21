using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using TimeForCode.Reporting.Commands;
using TimeForCode.Shared.Identifiers;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetProjectBadgeImageHandler : IRequestHandler<GetProjectBadgeImageQuery, Result<BadgeImageContent>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetProjectBadgeImageHandler> _logger;

        public GetProjectBadgeImageHandler(IMediator mediator, ILogger<GetProjectBadgeImageHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public Task<Result<BadgeImageContent>> Handle(GetProjectBadgeImageQuery request, CancellationToken cancellationToken)
        {
            // The Donation context answers not found for an unknown and for an unpublished project alike.
            // A bad request is not an unknown project (the identifier was valid): it is a defect, so it is logged.
            // An unavailable badge is shown as an image so a README never breaks, but not silently: the cause may be a defect of ours.
            return BadgeImageFlow.RunAsync(
                ObjectIdFormat.IsValid(request.ProjectId),
                BadgeImageState.NotFound,
                () => _mediator.Send(new GetProjectPublicSummaryQuery { ProjectId = request.ProjectId }, cancellationToken),
                summary => Task.FromResult(Render(request.Kind, summary)),
                reason => _logger.LogWarning("The badge of project {ProjectId} is unavailable: {Reason}", request.ProjectId, reason));
        }

        private static BadgeImageContent Render(ProjectBadgeKind kind, ProjectPublicSummaryResult summary)
        {
            if (kind == ProjectBadgeKind.Progress)
            {
                return new BadgeImageContent(BadgeImageState.Progress, $"{summary.HoursLogged} / {Hours(summary.HoursReceived)} ({Math.Min(summary.ProgressPercentage, 100)}%)");
            }

            return new BadgeImageContent(BadgeImageState.HoursReceived, Hours(summary.HoursReceived));
        }

        private static string Hours(int hours) => hours == 1 ? "1 hour" : $"{hours} hours";
    }
}