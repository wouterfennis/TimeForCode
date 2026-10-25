using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RestSharp;
using RichardSzalay.MockHttp;
using System.Net;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Infrastructure.Options;
using TimeForCode.Authorization.Infrastructure.Services;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class DonationServiceTests
    {
        private const string UserId = "5f43a0e74b12c84f1b0000c1";
        private const string AnonymiseUrl = "http://donation.test/api/v1/contributor/" + UserId + "/anonymise";

        private MockHttpMessageHandler _mockHttp = null!;
        private Mock<ITokenService> _tokenService = null!;
        private DonationService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockHttp = new MockHttpMessageHandler();
            _tokenService = new Mock<ITokenService>();
            _tokenService.Setup(x => x.GenerateInternalToken("authorization-service", "service", "service"))
                .Returns(new AccessToken { Token = "service-jwt", ExpiresAfter = DateTimeOffset.UtcNow.AddMinutes(1) });

            var restClient = new RestClient(new RestClientOptions { ConfigureMessageHandler = _ => _mockHttp });
            var options = Microsoft.Extensions.Options.Options.Create(new DonationOptions { BaseUri = "http://donation.test/" });

            _sut = new DonationService(options, restClient, _tokenService.Object, NullLogger<DonationService>.Instance);
        }

        [TestMethod]
        public async Task AnonymiseContributorAsync_DonationAccepts_SucceedsWithServiceToken()
        {
            _mockHttp.Expect(HttpMethod.Post, AnonymiseUrl)
                .WithHeaders("Authorization", "Bearer service-jwt")
                .Respond(HttpStatusCode.NoContent);

            var result = await _sut.AnonymiseContributorAsync(UserId);

            result.IsSuccess.Should().BeTrue();
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task AnonymiseContributorAsync_DonationFails_Fails()
        {
            _mockHttp.When(HttpMethod.Post, AnonymiseUrl).Respond(HttpStatusCode.InternalServerError);

            var result = await _sut.AnonymiseContributorAsync(UserId);

            result.IsFailure.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("not a uri")]
        public async Task AnonymiseContributorAsync_BaseUriMissingOrInvalid_FailsInsteadOfThrowing(string baseUri)
        {
            var restClient = new RestClient(new RestClientOptions { ConfigureMessageHandler = _ => _mockHttp });
            var options = Microsoft.Extensions.Options.Options.Create(new DonationOptions { BaseUri = baseUri });
            var sut = new DonationService(options, restClient, _tokenService.Object, NullLogger<DonationService>.Instance);

            var result = await sut.AnonymiseContributorAsync(UserId);

            result.IsFailure.Should().BeTrue();
        }

        [TestMethod]
        public async Task AnonymiseContributorAsync_DonationUnreachable_Fails()
        {
            _mockHttp.When(HttpMethod.Post, AnonymiseUrl).Throw(new HttpRequestException("connection refused"));

            var result = await _sut.AnonymiseContributorAsync(UserId);

            result.IsFailure.Should().BeTrue();
        }
    }
}