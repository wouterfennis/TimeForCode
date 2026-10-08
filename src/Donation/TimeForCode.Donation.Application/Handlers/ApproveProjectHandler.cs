using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class ApproveProjectHandler : IRequestHandler<ApproveProjectCommand, Result<ProjectLifecycleResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<ApproveProjectHandler> _logger;

        public ApproveProjectHandler(IProjectRepository projectRepository, ILogger<ApproveProjectHandler> logger)
        {
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<Result<ProjectLifecycleResult>> Handle(ApproveProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Approving project {ProjectId}", request.ProjectId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<ProjectLifecycleResult>.Failure("Project not found.");
            }

            var transition = project.Approve();
            if (!transition.IsSuccess)
            {
                _logger.LogWarning("Approval of project {ProjectId} rejected: {Error}", request.ProjectId, transition.ErrorMessage);
                return Result<ProjectLifecycleResult>.Conflict(transition.ErrorMessage);
            }

            await _projectRepository.UpdateAsync(project);

            return Result<ProjectLifecycleResult>.Success(new ProjectLifecycleResult
            {
                ProjectId = project.Id.ToString(),
                Status = project.Status
            });
        }
    }
}