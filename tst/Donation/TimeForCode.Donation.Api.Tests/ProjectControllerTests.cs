using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TimeForCode.Donation.Api.V1.Controllers;
using TimeForCode.Donation.Api.V1.Models;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Api.Tests
{
    [TestClass]
    public class ProjectControllerTests
    {
        private const string UserId = "user-1";
        private const string AvatarUrl = "https://avatars.githubusercontent.com/u/1";

        private readonly Mock<IMediator> _mediator = new();

        private ProjectController CreateSut(params Claim[] claims)
        {
            return new ProjectController(_mediator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
                }
            };
        }

        private static RegisterProjectRequest Request() => new() { GithubRepositoryUrl = new Uri("https://github.com/owner/repo") };

        [TestMethod]
        public async Task RegisterProject_TokenWithoutLogin_ReturnsBadRequestWithoutRegistering()
        {
            var sut = CreateSut(new Claim(ClaimTypes.NameIdentifier, UserId), new Claim("avatar_url", AvatarUrl));

            var response = await sut.RegisterProject(Request());

            response.Should().BeOfType<BadRequestObjectResult>();
            _mediator.Verify(m => m.Send(It.IsAny<RegisterProjectCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task RegisterProject_TokenWithoutAvatarUrl_ReturnsBadRequestWithoutRegistering()
        {
            var sut = CreateSut(new Claim(ClaimTypes.NameIdentifier, UserId), new Claim("login", "octocat"));

            var response = await sut.RegisterProject(Request());

            response.Should().BeOfType<BadRequestObjectResult>();
            _mediator.Verify(m => m.Send(It.IsAny<RegisterProjectCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task RegisterProject_TokenWithIdentity_PassesLoginAndAvatarToTheCommand()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<RegisterProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RegisterProjectResult>.Success(new RegisterProjectResult { ProjectId = "507f1f77bcf86cd799439011" }));
            var sut = CreateSut(
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim("login", "octocat"),
                new Claim("avatar_url", AvatarUrl));

            await sut.RegisterProject(Request());

            _mediator.Verify(
                m => m.Send(It.Is<RegisterProjectCommand>(c => c.UserId == UserId && c.GithubLogin == "octocat" && c.AvatarUrl == AvatarUrl), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}