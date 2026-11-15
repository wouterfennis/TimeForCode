using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Tests.Handlers
{
    [TestClass]
    public class ExchangeCodeHandlerTests
    {
        private const string Verifier = "verifier-0123456789-0123456789-0123456789-abc";
        private const string UserId = "65a000000000000000000001";
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        private static readonly string Challenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier)));

        private Mock<ILoginCodeRepository> _codes = null!;
        private Mock<ITokenService> _tokenService = null!;
        private Mock<IRefreshTokenService> _refreshTokenService = null!;
        private ExchangeCodeHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _codes = new Mock<ILoginCodeRepository>();
            _tokenService = new Mock<ITokenService>();
            _refreshTokenService = new Mock<IRefreshTokenService>();
            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);
            _tokenService.Setup(x => x.GenerateInternalTokenForSessionAsync(UserId))
                .ReturnsAsync(Result<AccessToken>.Success(new AccessToken { Token = "jwt", ExpiresAfter = Now.AddMinutes(15) }));
            _refreshTokenService.Setup(x => x.CreateRefreshTokenAsync(UserId))
                .ReturnsAsync(new Values.RefreshToken { Token = "refresh", ExpiresAfter = Now.AddDays(7) });

            _sut = new ExchangeCodeHandler(_codes.Object, _tokenService.Object, _refreshTokenService.Object, timeProvider.Object, NullLogger<ExchangeCodeHandler>.Instance);
        }

        private static LoginCode Code(DateTimeOffset expiresAfter, string challenge, bool isNewUser = false) => new()
        {
            Id = ObjectId.GenerateNewId(),
            CodeHash = RefreshTokenHasher.Hash("one-time-code"),
            UserId = UserId,
            IsNewUser = isNewUser,
            CodeChallenge = challenge,
            ExpiresAfter = expiresAfter
        };

        private static ExchangeCodeCommand Command(string verifier = Verifier) => new() { Code = "one-time-code", CodeVerifier = verifier };

        [TestMethod]
        public async Task Handle_ValidCodeAndVerifier_IssuesAccessAndRefreshToken()
        {
            _codes.Setup(x => x.TakeAsync(RefreshTokenHasher.Hash("one-time-code"))).ReturnsAsync(Code(Now.AddSeconds(30), Challenge, isNewUser: true));

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
            result.Value.RefreshToken.Token.Should().Be("refresh");
            result.Value.IsNewUser.Should().BeTrue();
        }

        [TestMethod]
        public async Task Handle_UnknownOrAlreadyUsedCode_FailsWithoutIssuingTokens()
        {
            _codes.Setup(x => x.TakeAsync(It.IsAny<string>())).ReturnsAsync((LoginCode?)null);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _refreshTokenService.Verify(x => x.CreateRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ExpiredCode_FailsWithoutIssuingTokens()
        {
            _codes.Setup(x => x.TakeAsync(It.IsAny<string>())).ReturnsAsync(Code(Now.AddSeconds(-1), Challenge));

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _refreshTokenService.Verify(x => x.CreateRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_AccessTokenCannotBeIssued_FailsWithItsErrorAndCreatesNoRefreshToken()
        {
            _codes.Setup(x => x.TakeAsync(It.IsAny<string>())).ReturnsAsync(Code(Now.AddSeconds(30), Challenge));
            _tokenService.Setup(x => x.GenerateInternalTokenForSessionAsync(UserId))
                .ReturnsAsync(Result<AccessToken>.Failure("User not found"));

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Be("User not found");
            _refreshTokenService.Verify(x => x.CreateRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_VerifierOfAnotherLogin_FailsWithoutIssuingTokens()
        {
            _codes.Setup(x => x.TakeAsync(It.IsAny<string>())).ReturnsAsync(Code(Now.AddSeconds(30), Challenge));

            var result = await _sut.Handle(Command("another-verifier-0123456789-0123456789-0123456789"), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _refreshTokenService.Verify(x => x.CreateRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_AnyCode_IsLookedUpByItsHashNeverPlain()
        {
            _codes.Setup(x => x.TakeAsync(It.IsAny<string>())).ReturnsAsync((LoginCode?)null);

            await _sut.Handle(Command(), CancellationToken.None);

            _codes.Verify(x => x.TakeAsync("one-time-code"), Times.Never);
            _codes.Verify(x => x.TakeAsync(RefreshTokenHasher.Hash("one-time-code")), Times.Once);
        }
    }
}