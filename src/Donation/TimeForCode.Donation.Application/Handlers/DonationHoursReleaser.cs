using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;

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
            var remaining = hours;
            var allocations = await _allocationRepository.GetByContributionIdAsync(contributionId, cancellationToken);

            foreach (var allocation in allocations.Reverse())
            {
                if (remaining == 0)
                {
                    return;
                }

                var take = Math.Min(allocation.Hours, remaining);
                if (!await ReleaseFromDonationAsync(allocation.DonationId, take, cancellationToken))
                {
                    _logger.LogError("{Hours} hours of contribution {ContributionId} could not be released from donation {DonationId}", take, contributionId, allocation.DonationId);
                    continue;
                }

                if (take == allocation.Hours)
                {
                    await _allocationRepository.DeleteAsync(allocation.Id, cancellationToken);
                }
                else
                {
                    await _allocationRepository.UpdateAsync(allocation.Reduce(take), cancellationToken);
                }

                remaining -= take;
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