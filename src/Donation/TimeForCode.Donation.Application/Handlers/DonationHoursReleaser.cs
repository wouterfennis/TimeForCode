using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class DonationHoursReleaser : IDonationHoursReleaser
    {
        private const int MaxReleaseAttempts = 3;

        private readonly IDonationRepository _donationRepository;
        private readonly IDonationAllocationRepository _allocationRepository;
        private readonly ILogger<DonationHoursReleaser> _logger;

        public DonationHoursReleaser(
            IDonationRepository donationRepository,
            IDonationAllocationRepository allocationRepository,
            ILogger<DonationHoursReleaser> logger)
        {
            _donationRepository = donationRepository;
            _allocationRepository = allocationRepository;
            _logger = logger;
        }

        /// <summary>
        /// The donation is saved before its allocation is shrunk or removed, so a failure in between leaves an allocation that is too large, never hours that are counted without one.
        /// An allocation whose donation could not be saved is kept and logged.
        /// </summary>
        public async Task ReleaseAsync(ObjectId contributionId, int hours, CancellationToken cancellationToken)
        {
            var allocations = await _allocationRepository.GetByContributionIdAsync(contributionId, cancellationToken);

            foreach (var release in DonationAllocation.PlanRelease(allocations, hours))
            {
                var allocation = release.Allocation;
                if (!await ReleaseFromDonationAsync(allocation.DonationId, release.Hours, cancellationToken))
                {
                    _logger.LogError("{Hours} hours of contribution {ContributionId} could not be released from donation {DonationId}", release.Hours, contributionId, allocation.DonationId);
                    continue;
                }

                if (release.Remaining is { } remaining)
                {
                    await _allocationRepository.UpdateAsync(remaining, cancellationToken);
                }
                else
                {
                    await _allocationRepository.DeleteAsync(allocation.Id, cancellationToken);
                }
            }
        }

        private async Task<bool> ReleaseFromDonationAsync(ObjectId donationId, int hours, CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxReleaseAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var donation = await _donationRepository.GetByIdAsync(donationId.ToString());
                if (donation == null)
                {
                    _logger.LogWarning("Donation {DonationId} no longer exists; nothing to release", donationId);
                    return true;
                }

                donation.ReleaseHours(hours);

                try
                {
                    await _donationRepository.UpdateAsync(donation);
                    return true;
                }
                catch (RepositoryConflictException ex)
                {
                    _logger.LogWarning(ex, "Donation {DonationId} changed while releasing hours, attempt {Attempt}", donationId, attempt);
                }
            }

            return false;
        }
    }
}