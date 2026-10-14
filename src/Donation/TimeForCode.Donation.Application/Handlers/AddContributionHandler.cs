using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class AddContributionHandler : IRequestHandler<AddContributionCommand, Result<AddContributionResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IContributorRepository _contributorRepository;
        private readonly IContributionRepository _contributionRepository;
        private readonly ILogger<AddContributionHandler> _logger;

        public AddContributionHandler(
            IProjectRepository projectRepository,
            IContributorRepository contributorRepository,
            IContributionRepository contributionRepository,
            ILogger<AddContributionHandler> logger)
        {
            _projectRepository = projectRepository;
            _contributorRepository = contributorRepository;
            _contributionRepository = contributionRepository;
            _logger = logger;
        }

        public async Task<Result<AddContributionResult>> Handle(AddContributionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Logging {Hours} hours on project {ProjectId} for user {UserId}", request.Hours, request.ProjectId, request.UserId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null || project.Status != ProjectStatus.Active)
            {
                _logger.LogWarning("Project {ProjectId} not found or not published", request.ProjectId);
                return Result<AddContributionResult>.Failure("Project not found.");
            }

            var contributor = await _contributorRepository.UpsertAsync(
                Contributor.Create(request.UserId, request.GithubLogin, request.AvatarUrl));

            var contribution = Contribution.Create(
                project.Id,
                contributor,
                request.Hours,
                request.GithubReference,
                DateTimeOffset.UtcNow);

            await _contributionRepository.CreateAsync(contribution);

            _logger.LogInformation("Contribution {ContributionId} logged on project {ProjectId}", contribution.Id, request.ProjectId);

            return Result<AddContributionResult>.Success(new AddContributionResult { Contribution = contribution.ToDto() });
        }
    }
}