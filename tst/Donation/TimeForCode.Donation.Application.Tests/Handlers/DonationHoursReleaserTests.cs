using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class DonationHoursReleaserTests
    {
        private static readonly ObjectId ContributionId = new("5f43a0e74b12c84f1b0000f1");

        private Mock<IDonationRepository> _donationRepository = default!;
        private Mock<IDonationAllocationRepository> _allocationRepository = default!;
        private DonationHoursReleaser _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationRepository = new Mock<IDonationRepository>();
            _allocationRepository = new Mock<IDonationAllocationRepository>();
            _sut = new DonationHoursReleaser(
                _donationRepository.Object,
                _allocationRepository.Object,
                NullLogger<DonationHoursReleaser>.Instance);
        }

        private DonationEntity GivenDonation(DonationStatus status, int committed, int logged)
        {
            var donation = DonationTestData.Build(status, committed: committed, logged: logged);
            _donationRepository.Setup(r => r.GetByIdAsync(donation.Id.ToString())).ReturnsAsync(donation);
            return donation;
        }

        private static DonationAllocation GivenAllocation(DonationEntity donation, int hours)
        {
            return DonationAllocation.Create(ContributionId, donation.Id, hours);
        }

        private void GivenAllocations(params DonationAllocation[] allocations)
        {
            _allocationRepository.Setup(r => r.GetByContributionIdAsync(ContributionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(allocations);
        }

        [TestMethod]
        public async Task ReleaseAsync_AllHoursOfCompletedDonation_ReopensDonationAndRemovesAllocation()
        {
            var donation = GivenDonation(DonationStatus.Completed, committed: 5, logged: 5);
            var allocation = GivenAllocation(donation, 5);
            GivenAllocations(allocation);

            await _sut.ReleaseAsync(ContributionId, 5, CancellationToken.None);

            donation.HoursLogged.Should().Be(0);
            donation.Status.Should().Be(DonationStatus.Active);
            _donationRepository.Verify(r => r.UpdateAsync(donation), Times.Once);
            _allocationRepository.Verify(r => r.DeleteAsync(allocation.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ReleaseAsync_PartOfAllocation_KeepsAllocationWithRemainingHours()
        {
            var donation = GivenDonation(DonationStatus.Completed, committed: 5, logged: 5);
            var allocation = GivenAllocation(donation, 5);
            GivenAllocations(allocation);

            await _sut.ReleaseAsync(ContributionId, 2, CancellationToken.None);

            donation.HoursLogged.Should().Be(3);
            _allocationRepository.Verify(
                r => r.UpdateAsync(It.Is<DonationAllocation>(a => a.Id == allocation.Id && a.Hours == 3), It.IsAny<CancellationToken>()),
                Times.Once);
            _allocationRepository.Verify(r => r.DeleteAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ReleaseAsync_HoursSpilledOverTwoDonations_ReleasesNewestAllocationFirst()
        {
            var first = GivenDonation(DonationStatus.Completed, committed: 3, logged: 3);
            var second = GivenDonation(DonationStatus.Active, committed: 4, logged: 2);
            GivenAllocations(GivenAllocation(first, 3), GivenAllocation(second, 2));

            await _sut.ReleaseAsync(ContributionId, 3, CancellationToken.None);

            second.HoursLogged.Should().Be(0);
            first.HoursLogged.Should().Be(2);
            first.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public async Task ReleaseAsync_ConflictOnFirstSave_ReloadsAndReleasesAgain()
        {
            var donation = GivenDonation(DonationStatus.Active, committed: 5, logged: 3);
            GivenAllocations(GivenAllocation(donation, 3));
            _donationRepository.SetupSequence(r => r.UpdateAsync(donation))
                .ThrowsAsync(new RepositoryConflictException("changed"))
                .Returns(Task.CompletedTask);

            await _sut.ReleaseAsync(ContributionId, 3, CancellationToken.None);

            _donationRepository.Verify(r => r.UpdateAsync(donation), Times.Exactly(2));
            _allocationRepository.Verify(r => r.DeleteAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ReleaseAsync_ConflictOnEveryAttempt_KeepsAllocation()
        {
            var donation = GivenDonation(DonationStatus.Active, committed: 5, logged: 3);
            GivenAllocations(GivenAllocation(donation, 3));
            _donationRepository.Setup(r => r.UpdateAsync(donation)).ThrowsAsync(new RepositoryConflictException("changed"));

            await _sut.ReleaseAsync(ContributionId, 3, CancellationToken.None);

            _allocationRepository.Verify(r => r.DeleteAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
            _allocationRepository.Verify(r => r.UpdateAsync(It.IsAny<DonationAllocation>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ReleaseAsync_NewestDonationCannotBeSaved_DoesNotTakeItsShareFromAnOlderAllocation()
        {
            var older = GivenDonation(DonationStatus.Active, committed: 5, logged: 3);
            var newer = GivenDonation(DonationStatus.Active, committed: 5, logged: 2);
            var olderAllocation = GivenAllocation(older, 3);
            GivenAllocations(olderAllocation, GivenAllocation(newer, 2));
            _donationRepository.Setup(r => r.UpdateAsync(newer)).ThrowsAsync(new RepositoryConflictException("changed"));

            await _sut.ReleaseAsync(ContributionId, 2, CancellationToken.None);

            older.HoursLogged.Should().Be(3);
            _allocationRepository.Verify(r => r.UpdateAsync(It.Is<DonationAllocation>(a => a.Id == olderAllocation.Id), It.IsAny<CancellationToken>()), Times.Never);
            _allocationRepository.Verify(r => r.DeleteAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ReleaseAsync_ContributionWithoutAllocations_ChangesNothing()
        {
            GivenAllocations();

            await _sut.ReleaseAsync(ContributionId, 3, CancellationToken.None);

            _donationRepository.Verify(r => r.UpdateAsync(It.IsAny<DonationEntity>()), Times.Never);
        }

        [TestMethod]
        public async Task ReleaseAsync_DonationNoLongerExists_RemovesAllocation()
        {
            var donation = DonationTestData.Build(DonationStatus.Active);
            var allocation = GivenAllocation(donation, 3);
            GivenAllocations(allocation);
            _donationRepository.Setup(r => r.GetByIdAsync(donation.Id.ToString())).ReturnsAsync((DonationEntity?)null);

            await _sut.ReleaseAsync(ContributionId, 3, CancellationToken.None);

            _allocationRepository.Verify(r => r.DeleteAsync(allocation.Id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}