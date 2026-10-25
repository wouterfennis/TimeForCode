using AwesomeAssertions;
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
    public class GetContributorProfileHandlerTests
    {
        private const string UserId = "user-1";
        private static readonly ObjectId ContributorId = new("5f43a0e74b12c84f1b0000c1");

        private Mock<IContributorRepository> _contributorRepository = default!;
        private GetContributorProfileHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _contributorRepository = new Mock<IContributorRepository>();
            _sut = new GetContributorProfileHandler(_contributorRepository.Object, NullLogger<GetContributorProfileHandler>.Instance);
        }

        private static Contributor ContributorOf(string platformUserId)
        {
            var contributor = new Contributor
            {
                Id = ContributorId,
                PlatformUserId = platformUserId,
                GithubLogin = "octocat",
                AvatarUrl = "https://avatars.githubusercontent.com/u/1"
            };
            contributor.UpdateProfile("Backend developer", ["C#"], 10, null);
            return contributor;
        }

        [TestMethod]
        public async Task Handle_OwnProfile_ReturnsTheProfile()
        {
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync(UserId)).ReturnsAsync(ContributorOf(UserId));

            var result = await _sut.Handle(new GetContributorProfileQuery { CallerUserId = UserId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Profile.Id.Should().Be(ContributorId.ToString());
            result.Value.Profile.Role.Should().Be("Backend developer");
            result.Value.Profile.Skills.Should().Equal("C#");
            result.Value.Profile.HoursAvailable.Should().Be(10);
        }

        [TestMethod]
        public async Task Handle_OwnProfileWithoutContributor_ReturnsNotFound()
        {
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync(UserId)).ReturnsAsync((Contributor?)null);

            var result = await _sut.Handle(new GetContributorProfileQuery { CallerUserId = UserId }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Handle_AdministratorRequestsAnotherProfile_ReturnsTheProfile()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync(ContributorOf("another-user"));

            var result = await _sut.Handle(
                new GetContributorProfileQuery { CallerUserId = "admin", IsAdministrator = true, ContributorId = ContributorId.ToString() },
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        public async Task Handle_UserRequestsAnotherProfile_ReturnsForbidden()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync(ContributorOf("another-user"));

            var result = await _sut.Handle(
                new GetContributorProfileQuery { CallerUserId = UserId, ContributorId = ContributorId.ToString() },
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task Handle_UserRequestsOwnProfileById_ReturnsTheProfile()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync(ContributorOf(UserId));

            var result = await _sut.Handle(
                new GetContributorProfileQuery { CallerUserId = UserId, ContributorId = ContributorId.ToString() },
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        public async Task Handle_UnknownContributor_ReturnsNotFound()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync((Contributor?)null);

            var result = await _sut.Handle(
                new GetContributorProfileQuery { CallerUserId = "admin", IsAdministrator = true, ContributorId = ContributorId.ToString() },
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}