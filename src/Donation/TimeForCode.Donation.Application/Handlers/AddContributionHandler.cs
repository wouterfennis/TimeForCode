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
        private readonly IDonationHoursCounter _hoursCounter;
        private readonly ILogger<AddContributionHandler> _logger;

        public AddContributionHandler(
            IProjectRepository projectRepository,
            IContributorRepository contributorRepository,
            IContributionRepository contributionRepository,
            IDonationHoursCounter hoursCounter,
            ILogger<AddContributionHandler> logger)
        {
            _projectRepository = projectRepository;
            _contributorRepository = contributorRepository;
            _contributionRepository = contributionRepository;
            _hoursCounter = hoursCounter;
            _logger = logger;
        }

        public async Task<Result<AddContributionResult>> Handle(AddContributionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Logging {Hours} hours on project {ProjectId} for user {UserId}", request.Hours, request.ProjectId, request.UserId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null || !project.IsPublished)
            {
                _logger.LogWarning("Project {ProjectId} not found or not published", request.ProjectId);
                return Result<AddContributionResult>.NotFound("Project not found.");
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
            await _hoursCounter.CountAsync(project.Id.ToString(), contribution.Id, request.Hours, cancellationToken);

            _logger.LogInformation("Contribution {ContributionId} logged on project {ProjectId}", contribution.Id, request.ProjectId);

            return Result<AddContributionResult>.Success(new AddContributionResult { Contribution = contribution.ToDto() });
        }
    }
}