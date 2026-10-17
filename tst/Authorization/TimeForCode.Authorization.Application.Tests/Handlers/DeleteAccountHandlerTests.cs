using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Tests.Handlers
{
    [TestClass]
    public class DeleteAccountHandlerTests
    {
        private const string UserId = "5f43a0e74b12c84f1b0000c1";

        private Mock<IAccountInformationRepository> _accountRepository = null!;
        private Mock<IRefreshTokenRepository> _refreshTokenRepository = null!;
        private Mock<IDonationService> _donationService = null!;
        private List<string> _calls = null!;
        private DeleteAccountHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _calls = [];
            _accountRepository = new Mock<IAccountInformationRepository>();
            _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            _donationService = new Mock<IDonationService>();

            _donationService.Setup(x => x.AnonymiseContributorAsync(UserId))
                .Callback(() => _calls.Add("anonymise"))
                .ReturnsAsync(Result<Unit>.Success(Unit.Value));
            _refreshTokenRepository.Setup(x => x.DeleteByUserIdAsync(UserId))
                .Callback(() => _calls.Add("refresh tokens"))
                .Returns(Task.CompletedTask);
            _accountRepository.Setup(x => x.DeleteByInternalIdAsync(UserId))
                .Callback(() => _calls.Add("account"))
                .Returns(Task.CompletedTask);

            _sut = new DeleteAccountHandler(
                _accountRepository.Object,
                _refreshTokenRepository.Object,
                _donationService.Object,
                NullLogger<DeleteAccountHandler>.Instance);
        }

        private static DeleteAccountCommand Command() => new() { UserId = UserId };

        private void GivenAccount()
        {
            _accountRepository.Setup(x => x.GetByInternalIdAsync(UserId))
                .ReturnsAsync(new AccountInformation
                {
                    Id = MongoDB.Bson.ObjectId.Parse(UserId),
                    IdentityProviderId = "1",
                    Login = "octocat",
                    NodeId = "node",
                    AvatarUrl = "https://avatars.githubusercontent.com/u/1",
                    Name = "Octo Cat",
                    Company = null,
                    Email = "octo@example.com",
                    Bio = null,
                    Location = null
                });
        }

        [TestMethod]
        public async Task Handle_ExistingAccount_AnonymisesThenRemovesRefreshTokensThenAccount()
        {
            GivenAccount();

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _calls.Should().Equal("anonymise", "refresh tokens", "account");
        }

        [TestMethod]
        public async Task Handle_UnknownAccount_FailsWithNotFoundAndChangesNothing()
        {
            _accountRepository.Setup(x => x.GetByInternalIdAsync(UserId)).ReturnsAsync((AccountInformation)null!);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            _calls.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Handle_DonationModuleFails_FailsWithBadGatewayAndKeepsTheAccount()
        {
            GivenAccount();
            _donationService.Setup(x => x.AnonymiseContributorAsync(UserId))
                .ReturnsAsync(Result<Unit>.Failure("unavailable"));

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            _calls.Should().BeEmpty();
        }
    }
}