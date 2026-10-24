using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetMyProjectsHandler : IRequestHandler<GetMyProjectsQuery, Result<GetMyProjectsResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<GetMyProjectsHandler> _logger;

        public GetMyProjectsHandler(
            IProjectRepository projectRepository,
            IDonationRepository donationRepository,
            ILogger<GetMyProjectsHandler> logger)
        {
            _projectRepository = projectRepository;
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<GetMyProjectsResult>> Handle(GetMyProjectsQuery request, CancellationToken cancellationToken)
        {
            var projects = await _projectRepository.GetByPublishedByUserIdAsync(request.UserId, cancellationToken);
            _logger.LogInformation("User {UserId} has {Count} projects", request.UserId, projects.Count);

            var donations = projects.Count == 0
                ? []
                : await _donationRepository.GetReceivedByProjectIdsAsync(projects.Select(p => p.Id.ToString()).ToList(), cancellationToken);
            var hoursByProject = donations
                .GroupBy(d => d.ProjectId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.HoursCommitted));

            return Result<GetMyProjectsResult>.Success(new GetMyProjectsResult
            {
                Projects = projects
                    .Select(p => new MaintainedProjectDto
                    {
                        Project = ToDto(p),
                        HoursDonated = hoursByProject.GetValueOrDefault(p.Id)
                    })
                    .ToList()
            });
        }

        private static ProjectDto ToDto(Project project)
        {
            return new ProjectDto
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
        }
    }
}