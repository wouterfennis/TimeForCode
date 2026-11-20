using MediatR;
using System.Net;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetProjectBadgeImageHandler : IRequestHandler<GetProjectBadgeImageQuery, Result<BadgeImageContent>>
    {
        private readonly IMediator _mediator;

        public GetProjectBadgeImageHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Result<BadgeImageContent>> Handle(GetProjectBadgeImageQuery request, CancellationToken cancellationToken)
        {
            Result<ProjectPublicSummaryResult> summary = await _mediator.Send(new GetProjectPublicSummaryQuery { ProjectId = request.ProjectId }, cancellationToken);
            if (summary.IsFailure)
            {
                // The Donation context answers not found for an unknown and for an unpublished project alike.
                return Result<BadgeImageContent>.Success(summary.FailureStatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest
                    ? new BadgeImageContent(BadgeImageState.NotFound)
                    : new BadgeImageContent(BadgeImageState.Unavailable));
            }

            string text = request.Kind == ProjectBadgeKind.Progress
                ? $"{summary.Value.HoursLogged} / {Hours(summary.Value.HoursReceived)} ({summary.Value.ProgressPercentage}%)"
                : Hours(summary.Value.HoursReceived);

            return Result<BadgeImageContent>.Success(new BadgeImageContent(request.Kind == ProjectBadgeKind.Progress ? BadgeImageState.Progress : BadgeImageState.HoursReceived, text));
        }

        private static string Hours(int hours) => hours == 1 ? "1 hour" : $"{hours} hours";
    }
}