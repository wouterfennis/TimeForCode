using AwesomeAssertions;
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
    public class MaintainerControllerTests
    {
        private readonly Mock<IMediator> _mediator = new();

        private MaintainerController CreateSut()
        {
            return new MaintainerController(_mediator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"))
                    }
                }
            };
        }

        [TestMethod]
        public async Task GetMyProjects_Success_ReturnsEachProjectWithItsHoursDonated()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<GetMyProjectsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetMyProjectsResult>.Success(new GetMyProjectsResult
                {
                    Projects = [new MaintainedProjectDto { Project = ProjectControllerTests.Dto(), HoursDonated = 12 }]
                }));

            var response = await CreateSut().GetMyProjects();

            var body = response.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<GetMyProjectsResponse>().Subject;
            var item = body.Projects.Should().ContainSingle().Subject;
            item.HoursDonated.Should().Be(12);
            ProjectControllerTests.AssertProject(item.Project);
        }
    }
}