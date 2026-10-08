using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class RequestProjectChangesHandler : IRequestHandler<RequestProjectChangesCommand, Result<ProjectLifecycleResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<RequestProjectChangesHandler> _logger;

        public RequestProjectChangesHandler(IProjectRepository projectRepository, ILogger<RequestProjectChangesHandler> logger)
        {
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<Result<ProjectLifecycleResult>> Handle(RequestProjectChangesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Requesting changes for project {ProjectId}", request.ProjectId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<ProjectLifecycleResult>.Failure("Project not found.");
            }

            var transition = project.RequestChanges(request.Reason);
            if (!transition.IsSuccess)
            {
                _logger.LogWarning("Change request for project {ProjectId} rejected: {Error}", request.ProjectId, transition.ErrorMessage);
                return Result<ProjectLifecycleResult>.Conflict(transition.ErrorMessage);
            }

            await _projectRepository.UpdateAsync(project);

            return Result<ProjectLifecycleResult>.Success(new ProjectLifecycleResult
            {
                ProjectId = project.Id.ToString(),
                Status = project.Status,
                ReviewerReason = project.ChangeRequestReason
            });
        }
    }
}