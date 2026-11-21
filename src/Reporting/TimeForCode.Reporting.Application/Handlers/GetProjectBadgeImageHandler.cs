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

        public async Task<Result<BadgeImageContent>> Handle(GetProjectBadgeImageQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectIdFormat.IsValid(request.ProjectId))
            {
                return Result<BadgeImageContent>.Success(new BadgeImageContent(BadgeImageState.NotFound));
            }

            Result<ProjectPublicSummaryResult> summary = await _mediator.Send(new GetProjectPublicSummaryQuery { ProjectId = request.ProjectId }, cancellationToken);
            if (summary.IsFailure)
            {
                // The Donation context answers not found for an unknown and for an unpublished project alike.
                // A bad request is not an unknown project (the identifier was valid): it is a defect, so it is logged below.
                if (summary.FailureStatusCode is HttpStatusCode.NotFound)
                {
                    return Result<BadgeImageContent>.Success(new BadgeImageContent(BadgeImageState.NotFound));
                }

                // Shown as "unavailable" so a README never breaks, but not silently: the cause may be a defect of ours.
                _logger.LogWarning("The badge of project {ProjectId} is unavailable: {Reason}", request.ProjectId, summary.ErrorMessage);
                return Result<BadgeImageContent>.Success(new BadgeImageContent(BadgeImageState.Unavailable));
            }

            string text = request.Kind == ProjectBadgeKind.Progress
                ? $"{summary.Value.HoursLogged} / {Hours(summary.Value.HoursReceived)} ({Math.Min(summary.Value.ProgressPercentage, 100)}%)"
                : Hours(summary.Value.HoursReceived);

            return Result<BadgeImageContent>.Success(new BadgeImageContent(request.Kind == ProjectBadgeKind.Progress ? BadgeImageState.Progress : BadgeImageState.HoursReceived, text));
        }

        private static string Hours(int hours) => hours == 1 ? "1 hour" : $"{hours} hours";
    }
}