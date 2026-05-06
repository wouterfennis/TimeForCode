using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class RepositorySteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private TryResponse<ICollection<RepositoryResponse>?, ApiException?>? _result = null;

        public RepositorySteps(IAuthClient authClient, IServiceProvider provider)
        {
            _authClient = authClient;
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
            _result = await _authClient.TryUserRepositoriesAsync();
        }

        [Then("A list of public repositories is returned with name, description, star count, language, and URL")]
        public void ThenAListOfPublicRepositoriesIsReturnedWithNameDescriptionStarCountLanguageAndUrl()
        {
            _result!.Exception.Should().BeNull();
            _result!.Response.Should().NotBeNull();
            _result!.Response.Should().NotBeEmpty();

            var repo = _result!.Response!.First();
            repo.Name.Should().NotBeNullOrEmpty();
            repo.Url.Should().NotBeNullOrEmpty();
        }

        [Then("The user is informed they must re-authenticate via GitHub")]
        public void ThenTheUserIsInformedTheyMustReAuthenticateViaGitHub()
        {
            _result!.Exception.Should().NotBeNull();
            _result!.Exception!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }
    }
}