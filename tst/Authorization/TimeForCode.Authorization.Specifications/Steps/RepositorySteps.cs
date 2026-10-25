using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Specifications.Mocking;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class RepositorySteps
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IAuthClient _authClient;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _provider;
        private readonly SessionContext _session;
        private TryResponse<ICollection<RepositoryResponse>?, ApiException?>? _listResult = null;
        private HttpResponseMessage? _singleRepoResponse = null;

        public RepositorySteps(IAuthClient authClient, HttpClient httpClient, IServiceProvider provider, SessionContext session)
        {
            _authClient = authClient;
            _httpClient = httpClient;
            _provider = provider;
            _session = session;
        }

        [Given("The user has no stored access token of the external platform")]
        public void GivenTheUserHasNoStoredAccessTokenOfTheExternalPlatform()
        {
            // No token is stored by design; only the external platform's public data is available.
            ExternalPlatformMocks.RegisterPublicRepositories(_provider.GetRequiredService<MockHttpMessageHandler>());
        }

        [Given("The user owns a private repository at the external platform")]
        public void GivenTheUserOwnsAPrivateRepositoryAtTheExternalPlatform()
        {
            ExternalPlatformMocks.RegisterPrivateRepository(_provider.GetRequiredService<MockHttpMessageHandler>());
        }

        [When("The user requests their repositories from the time for code platform")]
        public async Task WhenTheUserRequestsTheirRepositoriesFromTheTimeForCodePlatformAsync()
        {
            _listResult = await _authClient.TryUserRepositoriesAsync();
        }

        [When(@"The user requests repository ""(.*)"" from the time for code platform")]
        public async Task WhenTheUserRequestsRepositoryFromTheTimeForCodePlatform(string ownerSlashRepo)
        {
            _singleRepoResponse = await _httpClient.GetAsync($"api/v1/user/repositories/{ownerSlashRepo}");
            _session.LastResponse = _singleRepoResponse;
        }

        [Then("A list of public repositories is returned with name, description, star count, language, and URL")]
        public void ThenAListOfPublicRepositoriesIsReturnedWithNameDescriptionStarCountLanguageAndUrl()
        {
            _listResult!.Exception.Should().BeNull();
            _listResult!.Response.Should().NotBeNull();
            _listResult!.Response.Should().NotBeEmpty();

            var repo = _listResult!.Response!.First();
            repo.Name.Should().NotBeNullOrEmpty();
            repo.Url.Should().NotBeNullOrEmpty();
        }

        [Then("A single repository is returned with name, description, star count, language, and URL")]
        public async Task ThenASingleRepositoryIsReturnedWithNameDescriptionStarCountLanguageAndUrl()
        {
            _singleRepoResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await _singleRepoResponse.Content.ReadAsStringAsync();
            var repo = JsonSerializer.Deserialize<RepositoryResponse>(content, JsonOptions);

            repo.Should().NotBeNull();
            repo!.Name.Should().NotBeNullOrEmpty();
            repo.Url.Should().NotBeNullOrEmpty();
        }

        [Then("The private repository is not in the returned list")]
        public void ThenThePrivateRepositoryIsNotInTheReturnedList()
        {
            _listResult!.Exception.Should().BeNull();
            _listResult!.Response.Should().NotBeEmpty();
            _listResult!.Response!.Select(r => r.Name).Should().NotContain(ExternalPlatformMocks.PrivateRepository.Name);
        }
    }
}