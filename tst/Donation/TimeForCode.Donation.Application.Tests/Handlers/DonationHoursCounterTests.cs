using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class DonationHoursCounterTests
    {
        private static readonly ObjectId ContributionId = new("5f43a0e74b12c84f1b0000f1");

        [TestMethod]
        public async Task CountAsync_ClockAdvancesBetweenReads_AllocationAndDonationShareOneTimestamp()
        {
            var donation = DonationTestData.Build(DonationStatus.Active, committed: 10);
            var donationRepository = new Mock<IDonationRepository>();
            donationRepository
                .Setup(r => r.GetActiveByProjectIdAsync(DonationTestData.ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([donation]);
            DonationAllocation? created = null;
            var allocationRepository = new Mock<IDonationAllocationRepository>();
            allocationRepository
                .Setup(r => r.CreateAsync(It.IsAny<DonationAllocation>(), It.IsAny<CancellationToken>()))
                .Callback<DonationAllocation, CancellationToken>((a, _) => created = a)
                .Returns(Task.CompletedTask);
            var sut = new DonationHoursCounter(
                donationRepository.Object,
                allocationRepository.Object,
                NullLogger<DonationHoursCounter>.Instance,
                new AdvancingTimeProvider(DonationTestData.Now));

            await sut.CountAsync(DonationTestData.ProjectId.ToString(), ContributionId, 4, CancellationToken.None);

            created.Should().NotBeNull();
            created!.CreatedAt.Should().Be(donation.UpdatedAt!.Value);
        }

        private sealed class AdvancingTimeProvider : TimeProvider
        {
            private DateTimeOffset _now;

            public AdvancingTimeProvider(DateTimeOffset start)
            {
                _now = start;
            }

            public override DateTimeOffset GetUtcNow()
            {
                _now = _now.AddSeconds(1);
                return _now;
            }
        }
    }
}