using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class UnpublishProjectHandler : IRequestHandler<UnpublishProjectCommand, Result<UnpublishProjectResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<UnpublishProjectHandler> _logger;

        public UnpublishProjectHandler(IProjectRepository projectRepository, ILogger<UnpublishProjectHandler> logger)
        {
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<Result<UnpublishProjectResult>> Handle(UnpublishProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Unpublishing project {ProjectId} for user {UserId}", request.ProjectId, request.UserId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<UnpublishProjectResult>.Failure("Project not found.");
            }

            if (project.PublishedByUserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} does not have permission to unpublish project {ProjectId}", request.UserId, request.ProjectId);
                return Result<UnpublishProjectResult>.Forbidden("You do not have permission to unpublish this project.");
            }

            project.Status = ProjectStatus.Archived;
            await _projectRepository.UpdateAsync(project);

            _logger.LogInformation("Project {ProjectId} unpublished successfully", request.ProjectId);

            return Result<UnpublishProjectResult>.Success(new UnpublishProjectResult
            {
                ProjectId = project.Id.ToString()
            });
        }
    }
}