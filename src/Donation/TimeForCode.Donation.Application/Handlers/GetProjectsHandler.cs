using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetProjectsHandler : IRequestHandler<GetProjectsQuery, Result<GetProjectsResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<GetProjectsHandler> _logger;

        public GetProjectsHandler(IProjectRepository projectRepository, ILogger<GetProjectsHandler> logger)
        {
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<Result<GetProjectsResult>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting projects page {PageNumber} with page size {PageSize}", request.PageNumber, request.PageSize);

            var (projects, totalCount) = await _projectRepository.GetAllPublishedAsync(request.PageNumber, request.PageSize);

            _logger.LogDebug("Retrieved {Count} projects out of {TotalCount} total", projects.Count(), totalCount);

            var projectDtos = projects.Select(p => new ProjectDto
            {
                Id = p.Id.ToString(),
                Name = p.Snapshot.Name,
                FullName = p.Snapshot.FullName,
                Description = p.Snapshot.Description,
                GithubUrl = p.Snapshot.HtmlUrl,
                Language = p.Snapshot.Language,
                Topics = p.Snapshot.Topics,
                StargazersCount = p.Snapshot.StargazersCount,
                ForksCount = p.Snapshot.ForksCount,
                OpenIssuesCount = p.Snapshot.OpenIssuesCount,
                Homepage = p.Snapshot.Homepage,
                DefaultBranch = p.Snapshot.DefaultBranch,
                License = p.Snapshot.License,
                OwnerLogin = p.Snapshot.OwnerLogin,
                OwnerAvatarUrl = p.Snapshot.OwnerAvatarUrl,
                CreatedAt = p.Snapshot.CreatedAt,
                UpdatedAt = p.Snapshot.UpdatedAt,
                PushedAt = p.Snapshot.PushedAt,
                Status = p.Status
            }).ToList();

            return Result<GetProjectsResult>.Success(new GetProjectsResult
            {
                Projects = projectDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }
    }
}