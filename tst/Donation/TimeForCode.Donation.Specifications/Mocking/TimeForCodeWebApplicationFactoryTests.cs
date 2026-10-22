using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Mocking
{
    [TestClass]
    public class TimeForCodeWebApplicationFactoryTests
    {
        [TestMethod]
        public void ResetState_ContributorStored_RemovesIt()
        {
            using var factory = new TimeForCodeWebApplicationFactory();
            var contributors = factory.Services.GetRequiredService<InMemoryContributorRepository>();
            contributors.Add(Contributor.Create("user", "login", "https://avatars.example/1"));

            factory.ResetState();

            contributors.Contributors.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CreateClient_ManyRequestsInOneRun_AreNeverRateLimited()
        {
            using var factory = new TimeForCodeWebApplicationFactory();
            using HttpClient client = factory.CreateClient();
            var statusCodes = new List<System.Net.HttpStatusCode>();

            for (int i = 0; i < 70; i++)
            {
                using HttpResponseMessage response = await client.GetAsync("api/v1/project/507f1f77bcf86cd799439011/contribution");
                statusCodes.Add(response.StatusCode);
            }

            statusCodes.Should().NotContain(System.Net.HttpStatusCode.TooManyRequests);
        }

        [TestMethod]
        public void ResetState_MockInvoked_ForgetsTheInvocation()
        {
            using var factory = new TimeForCodeWebApplicationFactory();
            var projects = factory.Services.GetRequiredService<Mock<IProjectRepository>>();
            projects.Object.UpdateAsync(null!);

            factory.ResetState();

            projects.Invocations.Should().BeEmpty();
        }
    }
}