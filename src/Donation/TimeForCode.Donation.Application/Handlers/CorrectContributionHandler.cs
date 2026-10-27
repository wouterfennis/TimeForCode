using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class CorrectContributionHandler : IRequestHandler<CorrectContributionCommand, Result<ContributionChangedResult>>
    {
        private readonly IContributionRepository _contributionRepository;
        private readonly IContributorRepository _contributorRepository;
        private readonly IDonationHoursReleaser _hoursReleaser;
        private readonly ILogger<CorrectContributionHandler> _logger;

        public CorrectContributionHandler(
            IContributionRepository contributionRepository,
            IContributorRepository contributorRepository,
            IDonationHoursReleaser hoursReleaser,
            ILogger<CorrectContributionHandler> logger)
        {
            _contributionRepository = contributionRepository;
            _contributorRepository = contributorRepository;
            _hoursReleaser = hoursReleaser;
            _logger = logger;
        }

        public async Task<Result<ContributionChangedResult>> Handle(CorrectContributionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Correcting contribution {ContributionId} on project {ProjectId} for user {UserId}", request.ContributionId, request.ProjectId, request.UserId);

            var contribution = await _contributionRepository.GetByIdAsync(request.ProjectId, request.ContributionId, cancellationToken);
            if (contribution == null)
            {
                _logger.LogWarning("Contribution {ContributionId} not found on project {ProjectId}", request.ContributionId, request.ProjectId);
                return Result<ContributionChangedResult>.NotFound("Contribution not found.");
            }

            var contributor = await _contributorRepository.GetByIdAsync(contribution.ContributorId);
            if (!contribution.CanBeChangedBy(request.UserId, contributor, request.IsAdministrator))
            {
                _logger.LogWarning("User {UserId} may not change contribution {ContributionId}", request.UserId, request.ContributionId);
                return Result<ContributionChangedResult>.Forbidden("Only the contributor or an administrator can change a contribution.");
            }

            var corrected = contribution.Correct(request.UserId, request.Hours, request.GithubReference, DateTimeOffset.UtcNow);
            await _contributionRepository.UpdateAsync(corrected, cancellationToken);

            // TODO(review): hours added by an upward correction are not counted toward donations (DEBT-21); only lowered hours are released
            var lowered = contribution.Hours - request.Hours;
            if (lowered > 0)
            {
                await _hoursReleaser.ReleaseAsync(contribution.Id, lowered, cancellationToken);
            }

            return Result<ContributionChangedResult>.Success(new ContributionChangedResult { Contribution = corrected.ToDto() });
        }
    }
}