using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class DonationHoursCounter : IDonationHoursCounter
    {
        private const int MaxAllocationAttempts = 3;

        private readonly IDonationRepository _donationRepository;
        private readonly IDonationAllocationRepository _allocationRepository;
        private readonly ILogger<DonationHoursCounter> _logger;

        public DonationHoursCounter(
            IDonationRepository donationRepository,
            IDonationAllocationRepository allocationRepository,
            ILogger<DonationHoursCounter> logger)
        {
            _donationRepository = donationRepository;
            _allocationRepository = allocationRepository;
            _logger = logger;
        }

        /// <summary>
        /// A donation changed by someone else in the meantime is reloaded and the remaining hours are counted again; the contribution itself is already saved.
        /// Each allocation is saved before its donation is updated, so a counted hour always has an allocation; the allocation of an update that conflicted is removed again.
        /// </summary>
        public async Task CountAsync(string projectId, ObjectId contributionId, int hours, CancellationToken cancellationToken)
        {
            var remaining = hours;

            for (var attempt = 1; attempt <= MaxAllocationAttempts && remaining > 0; attempt++)
            {
                var donations = await _donationRepository.GetActiveByProjectIdAsync(projectId, cancellationToken);
                DonationAllocation? unconfirmed = null;

                try
                {
                    foreach (var counted in Domain.Donation.CountHours(donations, remaining))
                    {
                        unconfirmed = DonationAllocation.Create(contributionId, counted.Donation.Id, counted.Hours);
                        await _allocationRepository.CreateAsync(unconfirmed, cancellationToken);
                        await _donationRepository.UpdateAsync(counted.Donation);
                        unconfirmed = null;
                        remaining -= counted.Hours;
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