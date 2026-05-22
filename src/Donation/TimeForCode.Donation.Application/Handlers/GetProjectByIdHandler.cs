using MediatR;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetProjectByIdHandler : IRequestHandler<GetProjectByIdQuery, Result<GetProjectByIdResult>>
    {
        private readonly IProjectRepository _projectRepository;

        public GetProjectByIdHandler(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<Result<GetProjectByIdResult>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null || project.Status != ProjectStatus.Published)
            {
                return Result<GetProjectByIdResult>.Failure("Project not found.");
            }

            var dto = new ProjectDto
            {
                Id = project.Id.ToString(),
                Name = project.Snapshot.Name,
                FullName = project.Snapshot.FullName,
                Description = project.Snapshot.Description,
                GithubUrl = project.Snapshot.HtmlUrl,
                Language = project.Snapshot.Language,
                Topics = project.Snapshot.Topics,
                StargazersCount = project.Snapshot.StargazersCount,
                ForksCount = project.Snapshot.ForksCount,
                OpenIssuesCount = project.Snapshot.OpenIssuesCount,
                Homepage = project.Snapshot.Homepage,
                DefaultBranch = project.Snapshot.DefaultBranch,
                License = project.Snapshot.License,
                OwnerLogin = project.Snapshot.OwnerLogin,
                OwnerAvatarUrl = project.Snapshot.OwnerAvatarUrl,
                CreatedAt = project.Snapshot.CreatedAt,
                UpdatedAt = project.Snapshot.UpdatedAt,
                PushedAt = project.Snapshot.PushedAt,
                Status = project.Status
            };

            return Result<GetProjectByIdResult>.Success(new GetProjectByIdResult { Project = dto });
        }
    }
}