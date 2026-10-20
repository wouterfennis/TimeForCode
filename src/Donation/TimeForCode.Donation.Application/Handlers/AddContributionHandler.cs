using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class AddContributionHandler : IRequestHandler<AddContributionCommand, Result<AddContributionResult>>
    {
        private const int MaxAllocationAttempts = 3;

        private readonly IProjectRepository _projectRepository;
        private readonly IContributorRepository _contributorRepository;
        private readonly IContributionRepository _contributionRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly IDonationAllocationRepository _allocationRepository;
        private readonly ILogger<AddContributionHandler> _logger;

        public AddContributionHandler(
            IProjectRepository projectRepository,
            IContributorRepository contributorRepository,
            IContributionRepository contributionRepository,
            IDonationRepository donationRepository,
            IDonationAllocationRepository allocationRepository,
            ILogger<AddContributionHandler> logger)
        {
            _projectRepository = projectRepository;
            _contributorRepository = contributorRepository;
            _contributionRepository = contributionRepository;
            _donationRepository = donationRepository;
            _allocationRepository = allocationRepository;
            _logger = logger;
        }

        public async Task<Result<AddContributionResult>> Handle(AddContributionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Logging {Hours} hours on project {ProjectId} for user {UserId}", request.Hours, request.ProjectId, request.UserId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null || project.Status != ProjectStatus.Active)
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
            await CountTowardsDonationsAsync(project.Id.ToString(), contribution.Id, request.Hours, cancellationToken);

            _logger.LogInformation("Contribution {ContributionId} logged on project {ProjectId}", contribution.Id, request.ProjectId);

            return Result<AddContributionResult>.Success(new AddContributionResult { Contribution = contribution.ToDto() });
        }

        // Logged hours count toward the project's Active donations, oldest pledge first; what one donation cannot absorb spills over to the next.
        // A donation changed by someone else in the meantime is reloaded and the remaining hours are counted again; the contribution itself is already saved.
        // Each allocation is saved before its donation is updated, so a counted hour always has an allocation; the allocation of an update that conflicted is removed again.
        private async Task CountTowardsDonationsAsync(string projectId, ObjectId contributionId, int hours, CancellationToken cancellationToken)
        {
            var remaining = hours;

            for (var attempt = 1; attempt <= MaxAllocationAttempts && remaining > 0; attempt++)
            {
                var donations = await _donationRepository.GetActiveByProjectIdAsync(projectId, cancellationToken);
                DonationAllocation? unconfirmed = null;

                try
                {
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

                        unconfirmed = DonationAllocation.Create(contributionId, donation.Id, applied);
                        await _allocationRepository.CreateAsync(unconfirmed, cancellationToken);
                        await _donationRepository.UpdateAsync(donation);
                        unconfirmed = null;
                        remaining -= applied;
                    }

                    return;
                }
                catch (RepositoryConflictException ex)
                {
                    if (unconfirmed != null)
                    {
                        await _allocationRepository.DeleteAsync(unconfirmed.Id, cancellationToken);
                    }

                    _logger.LogWarning(ex, "Donation of project {ProjectId} changed while counting contribution {ContributionId}, attempt {Attempt}", projectId, contributionId, attempt);
                }
            }

            _logger.LogError("{Remaining} hours of contribution {ContributionId} on project {ProjectId} could not be counted toward donations", remaining, contributionId, projectId);
        }
    }
}