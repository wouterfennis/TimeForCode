using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class ChangeDonationHandlerTests
    {
        private Mock<IDonationRepository> _donationRepository = default!;
        private ActivateDonationHandler _activate = default!;
        private CancelDonationHandler _cancel = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationRepository = new Mock<IDonationRepository>();
            _activate = new ActivateDonationHandler(_donationRepository.Object, NullLogger<ActivateDonationHandler>.Instance);
            _cancel = new CancelDonationHandler(_donationRepository.Object, NullLogger<CancelDonationHandler>.Instance);
        }

        private DonationEntity GivenDonation(DonationStatus status)
        {
            var donation = DonationTestData.Build(status);
            _donationRepository.Setup(r => r.GetByIdAsync(donation.Id.ToString())).ReturnsAsync(donation);
            return donation;
        }

        [TestMethod]
        public async Task Activate_PledgerOfPendingDonation_ActivatesAndSaves()
        {
            var donation = GivenDonation(DonationStatus.Pending);

            var result = await _activate.Handle(new ActivateDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            donation.Status.Should().Be(DonationStatus.Active);
            result.Value.Donation.Status.Should().Be(DonationStatus.Active);
            _donationRepository.Verify(r => r.UpdateAsync(donation), Times.Once);
        }

        [TestMethod]
        public async Task Activate_Administrator_ActivatesDonationOfAnotherUser()
        {
            var donation = GivenDonation(DonationStatus.Pending);

            var result = await _activate.Handle(new ActivateDonationCommand { DonationId = donation.Id.ToString(), UserId = "admin", IsAdministrator = true }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public async Task Activate_OtherUser_ReturnsForbiddenAndKeepsState()
        {
            var donation = GivenDonation(DonationStatus.Pending);

            var result = await _activate.Handle(new ActivateDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-2" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
            donation.Status.Should().Be(DonationStatus.Pending);
            _donationRepository.Verify(r => r.UpdateAsync(It.IsAny<DonationEntity>()), Times.Never);
        }

        [TestMethod]
        public async Task Activate_AlreadyActive_ReturnsConflict()
        {
            var donation = GivenDonation(DonationStatus.Active);

            var result = await _activate.Handle(new ActivateDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-1" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
            _donationRepository.Verify(r => r.UpdateAsync(It.IsAny<DonationEntity>()), Times.Never);
        }

        [TestMethod]
        public async Task Activate_DonationChangedConcurrently_ReturnsConflict()
        {
            var donation = GivenDonation(DonationStatus.Pending);
            _donationRepository.Setup(r => r.UpdateAsync(donation)).ThrowsAsync(new RepositoryConflictException("changed"));

            var result = await _activate.Handle(new ActivateDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-1" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [TestMethod]
        public async Task Cancel_DonationChangedConcurrently_ReturnsConflict()
        {
            var donation = GivenDonation(DonationStatus.Active);
            _donationRepository.Setup(r => r.UpdateAsync(donation)).ThrowsAsync(new RepositoryConflictException("changed"));

            var result = await _cancel.Handle(new CancelDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-1" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [TestMethod]
        public async Task Activate_UnknownDonation_ReturnsNotFound()
        {
            _donationRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((DonationEntity?)null);

            var result = await _activate.Handle(new ActivateDonationCommand { DonationId = "5f43a0e74b12c84f1b0000ff", UserId = "user-1" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        [DataRow(DonationStatus.Pending)]
        [DataRow(DonationStatus.Active)]
        public async Task Cancel_PledgerOfOpenDonation_CancelsAndSaves(DonationStatus status)
        {
            var donation = GivenDonation(status);

            var result = await _cancel.Handle(new CancelDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            donation.Status.Should().Be(DonationStatus.Cancelled);
            _donationRepository.Verify(r => r.UpdateAsync(donation), Times.Once);
        }

        [TestMethod]
        public async Task Cancel_OtherUser_ReturnsForbidden()
        {
            var donation = GivenDonation(DonationStatus.Active);

            var result = await _cancel.Handle(new CancelDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-2" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        [DataRow(DonationStatus.Completed)]
        [DataRow(DonationStatus.Cancelled)]
        public async Task Cancel_FinishedDonation_ReturnsConflict(DonationStatus status)
        {
            var donation = GivenDonation(status);

            var result = await _cancel.Handle(new CancelDonationCommand { DonationId = donation.Id.ToString(), UserId = "user-1" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [TestMethod]
        public async Task Cancel_UnknownDonation_ReturnsNotFound()
        {
            _donationRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((DonationEntity?)null);

            var result = await _cancel.Handle(new CancelDonationCommand { DonationId = "5f43a0e74b12c84f1b0000ff", UserId = "user-1" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}