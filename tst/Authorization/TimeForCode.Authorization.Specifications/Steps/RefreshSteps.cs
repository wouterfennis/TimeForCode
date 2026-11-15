using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using IServiceProvider = System.IServiceProvider;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class RefreshSteps
    {
        private const string PlainRefreshToken = "token";

        private readonly IAuthClient _authClient;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _provider;
        private readonly SessionContext _session;
        private TryResponse<TokenResponseModel?, ApiException<ProblemDetails>?>? _result = null;
        private Domain.Entities.RefreshToken? _storedRefreshToken;

        public RefreshSteps(IAuthClient authClient, HttpClient httpClient, IServiceProvider provider, SessionContext session)
        {
            _authClient = authClient;
            _httpClient = httpClient;
            _provider = provider;
            _session = session;
        }

        [When("The user calls the refresh token endpoint")]
        public async Task WhenTheUserCallsTheRefreshTokenEndpointAsync()
        {
            _result = await _authClient.TryRefreshAsync(_session.RefreshToken!);
            _session.Tokens = _result.Response;
            _session.LastException = _result.Exception;
        }

        [When("The user calls the refresh token endpoint with a read-only request")]
        public async Task WhenTheUserCallsTheRefreshTokenEndpointWithAReadOnlyRequestAsync()
        {
            _session.ReadOnlyRequestResponse = await _httpClient.GetAsync("/api/v1/authentication/refresh");
        }

        [When("The user calls the refresh token endpoint without a refresh token")]
        public async Task WhenTheUserCallsTheRefreshTokenEndpointWithoutARefreshTokenAsync()
        {
            _session.LastResponse = await _httpClient.PostAsync("/api/v1/authentication/refresh", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        }

        [Then("The following refresh error message is returned: {string}")]
        public void ThenTheFollowingRefreshErrorMessageIsReturned(string errorMessage)
        {
            _result!.Exception.Should().NotBeNull();

            var problemDetails = _result!.Exception!.Result;
            problemDetails!.Detail.Should().NotBeNull();
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Given("The user has an expired refresh token")]
        public void GivenTheUserHasAnExpiredRefreshToken()
        {
            SeedRefreshToken(DateTime.UtcNow.AddHours(-1));
        }

        [Given("The user has a refresh token")]
        public void GivenTheUserHasARefreshToken()
        {
            SeedRefreshToken(DateTime.UtcNow.AddHours(1));
        }

        [Given("The user has already refreshed with that refresh token")]
        public void GivenTheUserHasAlreadyRefreshedWithThatRefreshToken()
        {
            _storedRefreshToken!.MarkRotated(DateTimeOffset.UtcNow.AddMinutes(-1));
        }

        [Then("All other refresh tokens of the user are revoked")]
        public void ThenAllOtherRefreshTokensOfTheUserAreRevoked()
        {
            var repository = _provider.GetRequiredService<Mock<IRefreshTokenRepository>>();
            repository.Verify(x => x.DeleteByUserIdAsync(_session.UserId), Times.Once);
        }

        [Then("The refresh token that was sent can no longer be used")]
        public void ThenTheRefreshTokenThatWasSentCanNoLongerBeUsed()
        {
            _session.Tokens!.RefreshToken.Token.Should().NotBe(PlainRefreshToken);
            _provider.GetRequiredService<Mock<IRefreshTokenRepository>>().Verify(x => x.TryMarkRotatedAsync(_storedRefreshToken!.Id, It.IsAny<DateTimeOffset>()), Times.Once);
        }

        private void SeedRefreshToken(DateTime expiresAfter)
        {
            _session.RefreshToken = new Api.Client.RefreshToken { Token = PlainRefreshToken, ExpiresAfter = expiresAfter };

            _storedRefreshToken = new Domain.Entities.RefreshToken
            {
                ExpiresAfter = expiresAfter,
                Id = new ObjectId(),
                TokenHash = RefreshTokenHasher.Hash(PlainRefreshToken),
                UserId = _session.UserId
            };

            var repository = _provider.GetService<Mock<IRefreshTokenRepository>>()!;
            repository.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash(PlainRefreshToken)))
                .ReturnsAsync(_storedRefreshToken);
            repository.Setup(x => x.TryMarkRotatedAsync(It.IsAny<ObjectId>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(true);
        }
    }
}