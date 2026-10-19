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
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<AddContributionHandler> _logger;

        public AddContributionHandler(
            IProjectRepository projectRepository,
            IContributorRepository contributorRepository,
            IContributionRepository contributionRepository,
            IDonationRepository donationRepository,
            ILogger<AddContributionHandler> logger)
        {
            _projectRepository = projectRepository;
            _contributorRepository = contributorRepository;
            _contributionRepository = contributionRepository;
            _donationRepository = donationRepository;
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
            await CountTowardsDonationsAsync(project.Id.ToString(), request.Hours, cancellationToken);

            _logger.LogInformation("Contribution {ContributionId} logged on project {ProjectId}", contribution.Id, request.ProjectId);

            return Result<AddContributionResult>.Success(new AddContributionResult { Contribution = contribution.ToDto() });
        }

        // Logged hours count toward the project's Active donations, oldest pledge first; what one donation cannot absorb spills over to the next.
        private async Task CountTowardsDonationsAsync(string projectId, int hours, CancellationToken cancellationToken)
        {
            var remaining = hours;
            var donations = await _donationRepository.GetByProjectIdAsync(projectId, cancellationToken);

            foreach (var donation in donations)
            {
                if (remaining == 0)
                {
                    break;
                }

                var applied = donation.LogHours(remaining);
                if (applied == 0)
                {
                    continue;
                }

                remaining -= applied;
                await _donationRepository.UpdateAsync(donation);
            }
        }
    }
}