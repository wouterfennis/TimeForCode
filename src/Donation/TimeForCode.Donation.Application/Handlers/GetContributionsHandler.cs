using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetContributionsHandler : IRequestHandler<GetContributionsQuery, Result<GetContributionsResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IContributionRepository _contributionRepository;
        private readonly ILogger<GetContributionsHandler> _logger;

        public GetContributionsHandler(
            IProjectRepository projectRepository,
            IContributionRepository contributionRepository,
            ILogger<GetContributionsHandler> logger)
        {
            _projectRepository = projectRepository;
            _contributionRepository = contributionRepository;
            _logger = logger;
        }

        public async Task<Result<GetContributionsResult>> Handle(GetContributionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting contributions of project {ProjectId}", request.ProjectId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null || project.Status != ProjectStatus.Active)
            {
                _logger.LogWarning("Project {ProjectId} not found or not published", request.ProjectId);
                return Result<GetContributionsResult>.Failure("Project not found.");
            }

            var contributions = await _contributionRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

            return Result<GetContributionsResult>.Success(new GetContributionsResult
            {
                Contributions = contributions
                    .OrderBy(c => c.LoggedAt)
                    .ThenBy(c => c.Id)
                    .Select(c => c.ToDto())
                    .ToList()
            });
        }
    }
}