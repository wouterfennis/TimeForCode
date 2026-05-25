using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class RepositorySteps
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IAuthClient _authClient;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _provider;
        private TryResponse<ICollection<RepositoryResponse>?, ApiException?>? _listResult = null;
        private HttpResponseMessage? _singleRepoResponse = null;

        public RepositorySteps(IAuthClient authClient, HttpClient httpClient, IServiceProvider provider)
        {
            _authClient = authClient;
            _httpClient = httpClient;
            _provider = provider;
        }

        [Given("The user's GitHub access token has been revoked at the external platform")]
        public void GivenTheUsersGitHubAccessTokenHasBeenRevokedAtTheExternalPlatform()
        {
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();
            mockHttp.When("http://localhost:8081/user/repos")
                .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"title\":\"Unauthorized\"}");
        }

        [When("The user requests their repositories from the time for code platform")]
        public async Task WhenTheUserRequestsTheirRepositoriesFromTheTimeForCodePlatformAsync()
        {
            _listResult = await _authClient.TryUserRepositoriesAsync();
        }

        [When(@"The user requests repository ""(.*)"" from the time for code platform")]
        public async Task WhenTheUserRequestsRepositoryFromTheTimeForCodePlatform(string ownerSlashRepo)
        {
            _singleRepoResponse = await _httpClient.GetAsync($"api/user/repositories/{ownerSlashRepo}");
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

        [Then("The user is informed they must re-authenticate via GitHub")]
        public void ThenTheUserIsInformedTheyMustReAuthenticateViaGitHub()
        {
            _listResult!.Exception.Should().NotBeNull();
            _listResult!.Exception!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }
    }
}