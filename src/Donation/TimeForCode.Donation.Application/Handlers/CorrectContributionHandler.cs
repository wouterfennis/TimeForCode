using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class CorrectContributionHandler : IRequestHandler<CorrectContributionCommand, Result<ContributionChangedResult>>
    {
        private readonly IContributionRepository _contributionRepository;
        private readonly IContributorRepository _contributorRepository;
        private readonly IDonationHoursCounter _hoursCounter;
        private readonly IDonationHoursReleaser _hoursReleaser;
        private readonly ILogger<CorrectContributionHandler> _logger;

        public CorrectContributionHandler(
            IContributionRepository contributionRepository,
            IContributorRepository contributorRepository,
            IDonationHoursCounter hoursCounter,
            IDonationHoursReleaser hoursReleaser,
            ILogger<CorrectContributionHandler> logger)
        {
            _contributionRepository = contributionRepository;
            _contributorRepository = contributorRepository;
            _hoursCounter = hoursCounter;
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
            var change = contribution.Correct(request.UserId, request.IsAdministrator, contributor, request.Hours, request.GithubReference, DateTimeOffset.UtcNow);
            if (!change.IsSuccess)
            {
                _logger.LogWarning("Contribution {ContributionId} was not corrected for user {UserId}: {Failure}", request.ContributionId, request.UserId, change.Failure);
                return change.ToFailure();
            }

            try
            {
                await _contributionRepository.UpdateAsync(change.Contribution!, cancellationToken);
            }
            catch (RepositoryConflictException ex)
            {
                _logger.LogWarning(ex, "Contribution {ContributionId} was changed concurrently", request.ContributionId);
                return ContributionChangeMapper.Conflict();
            }

            if (change.HoursDelta > 0)
            {
                await _hoursCounter.CountAsync(contribution.ProjectId.ToString(), contribution.Id, change.HoursDelta, cancellationToken);
            }
            else if (change.HoursDelta < 0)
            {
                await _hoursReleaser.ReleaseAsync(contribution.Id, -change.HoursDelta, cancellationToken);
            }

            return Result<ContributionChangedResult>.Success(new ContributionChangedResult { Contribution = change.Contribution!.ToDto() });
        }
    }
}