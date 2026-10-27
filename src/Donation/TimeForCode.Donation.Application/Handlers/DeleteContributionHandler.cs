using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class DeleteContributionHandler : IRequestHandler<DeleteContributionCommand, Result<ContributionChangedResult>>
    {
        private readonly IContributionRepository _contributionRepository;
        private readonly IContributorRepository _contributorRepository;
        private readonly IDonationHoursReleaser _hoursReleaser;
        private readonly ILogger<DeleteContributionHandler> _logger;

        public DeleteContributionHandler(
            IContributionRepository contributionRepository,
            IContributorRepository contributorRepository,
            IDonationHoursReleaser hoursReleaser,
            ILogger<DeleteContributionHandler> logger)
        {
            _contributionRepository = contributionRepository;
            _contributorRepository = contributorRepository;
            _hoursReleaser = hoursReleaser;
            _logger = logger;
        }

        public async Task<Result<ContributionChangedResult>> Handle(DeleteContributionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting contribution {ContributionId} on project {ProjectId} for user {UserId}", request.ContributionId, request.ProjectId, request.UserId);

            var contribution = await _contributionRepository.GetByIdAsync(request.ProjectId, request.ContributionId, cancellationToken);
            if (contribution == null)
            {
                _logger.LogWarning("Contribution {ContributionId} not found on project {ProjectId}", request.ContributionId, request.ProjectId);
                return Result<ContributionChangedResult>.NotFound("Contribution not found.");
            }

            var contributor = await _contributorRepository.GetByIdAsync(contribution.ContributorId);
            var change = contribution.MarkDeleted(request.UserId, request.IsAdministrator, contributor, DateTimeOffset.UtcNow);
            if (!change.IsSuccess)
            {
                _logger.LogWarning("Contribution {ContributionId} was not deleted for user {UserId}: {Failure}", request.ContributionId, request.UserId, change.Failure);
                return change.ToFailure();
            }

            await _contributionRepository.UpdateAsync(change.Contribution!, cancellationToken);
            await _hoursReleaser.ReleaseAsync(contribution.Id, -change.HoursDelta, cancellationToken);

            return Result<ContributionChangedResult>.Success(new ContributionChangedResult { Contribution = change.Contribution!.ToDto() });
        }
    }
}