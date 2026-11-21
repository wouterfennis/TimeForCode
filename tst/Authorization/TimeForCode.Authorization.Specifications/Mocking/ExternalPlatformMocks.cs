using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Infrastructure.Services.Github;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    internal static class ExternalPlatformMocks
    {
        private const string BaseUrl = "http://localhost:8081";

        public static GithubRepository PublicRepository => new()
        {
            Name = "my-repo",
            Description = "A test repository",
            StargazersCount = 42,
            Language = "C#",
            HtmlUrl = "https://github.com/johndoe/my-repo",
            IsPrivate = false
        };

        public static GithubRepository PrivateRepository => new()
        {
            Name = "secret-repo",
            Description = "A private repository",
            StargazersCount = 0,
            Language = "C#",
            HtmlUrl = "https://github.com/johndoe/secret-repo",
            IsPrivate = true
        };

        /// <summary>Makes the external platform answer a request to <paramref name="path"/> with 404 and a problem detail.</summary>
        public static void RegisterNotFound(MockHttpMessageHandler mockHttp, string path, string message)
        {
            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = message,
                Status = (int)HttpStatusCode.NotFound,
                Detail = message
            };

            mockHttp.When($"{BaseUrl}{path}")
                .Respond(HttpStatusCode.NotFound, "application/json", JsonSerializer.Serialize(problemDetails));
        }

        public static void RegisterPublicRepositories(MockHttpMessageHandler mockHttp)
        {
            mockHttp.When($"{BaseUrl}/users/johndoe/repos*")
                .Respond("application/json", JsonSerializer.Serialize(new[] { PublicRepository }));
            mockHttp.When($"{BaseUrl}/repos/*")
                .Respond("application/json", JsonSerializer.Serialize(PublicRepository));
        }

        public static void RegisterPrivateRepository(MockHttpMessageHandler mockHttp)
        {
            mockHttp.When($"{BaseUrl}/users/johndoe/repos*")
                .Respond("application/json", JsonSerializer.Serialize(new[] { PublicRepository, PrivateRepository }));
            mockHttp.When($"{BaseUrl}/repos/johndoe/secret-repo")
                .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"Not Found\"}");
            mockHttp.When($"{BaseUrl}/repos/*")
                .Respond("application/json", JsonSerializer.Serialize(PublicRepository));
        }
    }
}