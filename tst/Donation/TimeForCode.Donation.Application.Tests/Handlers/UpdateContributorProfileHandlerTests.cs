using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class UpdateContributorProfileHandlerTests
    {
        private const string UserId = "user-1";
        private static readonly ObjectId ContributorId = new("5f43a0e74b12c84f1b0000c1");
        private static readonly ObjectId OrganizationId = new("5f43a0e74b12c84f1b0000d1");

        private Mock<IContributorRepository> _contributorRepository = default!;
        private Mock<IDonorOrganizationRepository> _organizationRepository = default!;
        private UpdateContributorProfileHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _contributorRepository = new Mock<IContributorRepository>();
            _organizationRepository = new Mock<IDonorOrganizationRepository>();

            _sut = new UpdateContributorProfileHandler(
                _contributorRepository.Object,
                _organizationRepository.Object,
                NullLogger<UpdateContributorProfileHandler>.Instance);
        }

        private static Contributor ContributorOf(string platformUserId) => new()
        {
            Id = ContributorId,
            PlatformUserId = platformUserId,
            GithubLogin = "octocat",
            AvatarUrl = "https://avatars.githubusercontent.com/u/1"
        };

        private static UpdateContributorProfileCommand Command(
            string? contributorId = null,
            bool isAdministrator = false,
            string callerUserId = UserId,
            string? role = "Backend developer",
            List<string>? skills = null,
            int? hoursAvailable = 10,
            string? donorOrganizationId = null) => new()
            {
                CallerUserId = callerUserId,
                GithubLogin = "octocat",
                AvatarUrl = "https://avatars.githubusercontent.com/u/1",
                IsAdministrator = isAdministrator,
                ContributorId = contributorId,
                Role = role,
                Skills = skills ?? ["C#", "MongoDB"],
                HoursAvailable = hoursAvailable,
                DonorOrganizationId = donorOrganizationId
            };

        private void GivenContributorOfCaller()
        {
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync(UserId)).ReturnsAsync(ContributorOf(UserId));
        }

        [TestMethod]
        public async Task Handle_OwnProfile_UpdatesTheExistingContributor()
        {
            GivenContributorOfCaller();

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Profile.Role.Should().Be("Backend developer");
            result.Value.Profile.Skills.Should().Equal("C#", "MongoDB");
            result.Value.Profile.HoursAvailable.Should().Be(10);
            _contributorRepository.Verify(r => r.UpdateProfileAsync(
                It.Is<Contributor>(c => c.Id == ContributorId && c.Role == "Backend developer"),
                It.IsAny<CancellationToken>()), Times.Once);
            _contributorRepository.Verify(r => r.UpsertAsync(It.IsAny<Contributor>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_OwnProfileWithoutContributor_RegistersTheContributorFromTheToken()
        {
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync(UserId)).ReturnsAsync((Contributor?)null);
            _contributorRepository.Setup(r => r.UpsertAsync(It.IsAny<Contributor>()))
                .ReturnsAsync((Contributor c) => c);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _contributorRepository.Verify(r => r.UpsertAsync(It.Is<Contributor>(c =>
                c.PlatformUserId == UserId && c.GithubLogin == "octocat")), Times.Once);
            _contributorRepository.Verify(r => r.UpdateProfileAsync(
                It.Is<Contributor>(c => c.Role == "Backend developer"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_LinkToExistingOrganization_SetsTheAffiliation()
        {
            GivenContributorOfCaller();
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync(DonorOrganization.Create("Acme", null, null));

            var result = await _sut.Handle(Command(donorOrganizationId: OrganizationId.ToString()), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Profile.DonorOrganizationId.Should().Be(OrganizationId.ToString());
        }

        [TestMethod]
        public async Task Handle_LinkToUnknownOrganization_ReturnsNotFoundAndSavesNothing()
        {
            GivenContributorOfCaller();
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync((DonorOrganization?)null);

            var result = await _sut.Handle(Command(donorOrganizationId: OrganizationId.ToString()), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            _contributorRepository.Verify(r => r.UpdateProfileAsync(It.IsAny<Contributor>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_AdministratorUpdatesAnotherProfile_Succeeds()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync(ContributorOf("another-user"));

            var result = await _sut.Handle(
                Command(contributorId: ContributorId.ToString(), isAdministrator: true, callerUserId: "admin"),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _contributorRepository.Verify(r => r.UpdateProfileAsync(
                It.Is<Contributor>(c => c.Id == ContributorId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_UserUpdatesAnotherProfile_ReturnsForbiddenAndSavesNothing()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync(ContributorOf("another-user"));

            var result = await _sut.Handle(Command(contributorId: ContributorId.ToString()), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
            _contributorRepository.Verify(r => r.UpdateProfileAsync(It.IsAny<Contributor>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_UserUpdatesOwnProfileById_Succeeds()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync(ContributorOf(UserId));

            var result = await _sut.Handle(Command(contributorId: ContributorId.ToString()), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        public async Task Handle_UnknownContributor_ReturnsNotFound()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync((Contributor?)null);

            var result = await _sut.Handle(
                Command(contributorId: ContributorId.ToString(), isAdministrator: true, callerUserId: "admin"),
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}