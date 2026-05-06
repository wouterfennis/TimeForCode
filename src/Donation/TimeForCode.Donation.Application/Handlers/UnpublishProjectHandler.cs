using MediatR;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class UnpublishProjectHandler : IRequestHandler<UnpublishProjectCommand, Result<UnpublishProjectResult>>
    {
        private readonly IProjectRepository _projectRepository;

        public UnpublishProjectHandler(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<Result<UnpublishProjectResult>> Handle(UnpublishProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                return Result<UnpublishProjectResult>.Failure("Project not found.");
            }

            project.Status = ProjectStatus.Archived;
            await _projectRepository.UpdateAsync(project);

            return Result<UnpublishProjectResult>.Success(new UnpublishProjectResult
            {
                ProjectId = project.Id.ToString()
            });
        }
    }
}
