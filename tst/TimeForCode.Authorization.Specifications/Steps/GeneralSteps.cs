using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class GeneralSteps
    {
        private readonly IServiceProvider _provider;

        public GeneralSteps(IServiceProvider provider)
        {
            _provider = provider;
        }

        [Given("The user has an account at the external platform")]
        public void GivenTheUserHasAnAccountAtTheExternalPlatform()
        {
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

            var accountInformation = new AccountInformation
            {
                Id = ObjectId.GenerateNewId(),
                Name = "John Doe",
                Email = "",
                AvatarUrl = "http://avatars.githubusercontent.com/u/1?v=4",
                Login = "johndoe",
                NodeId = "",
                Company = "",
            };

            mockHttp.When("http://localhost:8081/user")
                    .Respond("application/json", JsonSerializer.Serialize(accountInformation));
        }

        [Given("The user has no account at the external platform")]
        public void GivenTheUserHasNoAccountAtTheExternalPlatform()
        {
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

            var problemDetails = new ProblemDetails
            {
                Title = "No account information",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "No account information"
            };

            mockHttp.When("http://localhost:8081/user")
                    .Respond(HttpStatusCode.NotFound, "application/json", JsonSerializer.Serialize(problemDetails));
        }

        [Given("The user logs in at the external platform")]
        public void GivenTheUserLogsInAtTheExternalPlatform()
        {
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

            var accessTokenResult = new GetAccessTokenResult
            {
                AccessToken = "token",
                Scope = "scope",
                TokenType = "token_type"
            };

            mockHttp.When("http://localhost:8081/login/oauth/access_token")
                .Respond("application/json", JsonSerializer.Serialize(accessTokenResult));
        }

        [Given("The user has not logged in at the external platform")]
        public void GivenTheUserHasNotLoggedInAtTheExternalPlatform()
        {
            // No need to do anything here
        }

    }
}