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