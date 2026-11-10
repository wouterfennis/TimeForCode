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

        [TestMethod]
        public async Task RegisterProject_Success_ReturnsCreatedWithTheProjectId()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<RegisterProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RegisterProjectResult>.Success(new RegisterProjectResult { ProjectId = "507f1f77bcf86cd799439011" }));
            var sut = CreateSut(
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim("login", "octocat"),
                new Claim("avatar_url", AvatarUrl));

            var response = await sut.RegisterProject(Request());

            response.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeOfType<RegisterProjectResponse>()
                .Which.ProjectId.Should().Be("507f1f77bcf86cd799439011");
        }

        [TestMethod]
        public async Task GetMaintainedProject_Success_ReturnsTheProjectAndReviewerReason()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<GetMaintainedProjectQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetMaintainedProjectResult>.Success(new GetMaintainedProjectResult { Project = Dto(), ReviewerReason = "Add a README" }));
            var sut = CreateSut(new Claim(ClaimTypes.NameIdentifier, UserId));

            var response = await sut.GetMaintainedProject("507f1f77bcf86cd799439011");

            var body = response.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<GetMaintainedProjectResponse>().Subject;
            body.ReviewerReason.Should().Be("Add a README");
            AssertProject(body.Project);
        }

        [TestMethod]
        public async Task ApproveProject_Success_ReturnsTheLifecycleState()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<ApproveProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectLifecycleResult>.Success(new ProjectLifecycleResult { ProjectId = "507f1f77bcf86cd799439011", Status = Values.ProjectStatus.Active, ReviewerReason = "Looks good" }));
            var sut = CreateSut();

            var response = await sut.ApproveProject("507f1f77bcf86cd799439011");

            var body = response.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ProjectLifecycleResponse>().Subject;
            body.ProjectId.Should().Be("507f1f77bcf86cd799439011");
            body.Status.Should().Be(Values.ProjectStatus.Active);
            body.ReviewerReason.Should().Be("Looks good");
        }

        [TestMethod]
        public async Task SubmitProjectForReview_Success_ReturnsTheLifecycleState()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<SubmitProjectForReviewCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lifecycle(Values.ProjectStatus.PendingApproval));

            var response = await CreateSut(new Claim(ClaimTypes.NameIdentifier, UserId)).SubmitProjectForReview("507f1f77bcf86cd799439011");

            AssertLifecycle(response, Values.ProjectStatus.PendingApproval);
        }

        [TestMethod]
        public async Task RequestProjectChanges_Success_ReturnsTheLifecycleState()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<RequestProjectChangesCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lifecycle(Values.ProjectStatus.Draft));

            var response = await CreateSut().RequestProjectChanges("507f1f77bcf86cd799439011", new RequestProjectChangesRequest { Reason = "Add a README" });

            AssertLifecycle(response, Values.ProjectStatus.Draft);
        }

        [TestMethod]
        public async Task ArchiveProject_Success_ReturnsTheLifecycleState()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<ArchiveProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lifecycle(Values.ProjectStatus.Archived));

            var response = await CreateSut(new Claim(ClaimTypes.NameIdentifier, UserId)).ArchiveProject("507f1f77bcf86cd799439011");

            AssertLifecycle(response, Values.ProjectStatus.Archived);
        }

        [TestMethod]
        public async Task ReactivateProject_Success_ReturnsTheLifecycleState()
        {
            _mediator
                .Setup(m => m.Send(It.IsAny<ReactivateProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lifecycle(Values.ProjectStatus.Active));

            var response = await CreateSut(new Claim(ClaimTypes.NameIdentifier, UserId)).ReactivateProject("507f1f77bcf86cd799439011");

            AssertLifecycle(response, Values.ProjectStatus.Active);
        }

        private static Result<ProjectLifecycleResult> Lifecycle(Values.ProjectStatus status) =>
            Result<ProjectLifecycleResult>.Success(new ProjectLifecycleResult { ProjectId = "507f1f77bcf86cd799439011", Status = status });

        private static void AssertLifecycle(IActionResult response, Values.ProjectStatus status)
        {
            var body = response.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ProjectLifecycleResponse>().Subject;
            body.ProjectId.Should().Be("507f1f77bcf86cd799439011");
            body.Status.Should().Be(status);
        }

        internal static ProjectDto Dto() => new()
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "repo",
            FullName = "owner/repo",
            Description = "A repository",
            GithubUrl = "https://github.com/owner/repo",
            Language = "C#",
            Topics = ["dotnet", "oss"],
            StargazersCount = 5,
            ForksCount = 2,
            OpenIssuesCount = 1,
            Homepage = "https://example.org",
            DefaultBranch = "main",
            License = "MIT",
            OwnerLogin = "owner",
            OwnerAvatarUrl = AvatarUrl,
            CreatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero),
            PushedAt = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero),
            Status = Values.ProjectStatus.Draft
        };

        internal static void AssertProject(ProjectResponse actual)
        {
            var expected = Dto();
            actual.Should().BeEquivalentTo(expected);
        }
    }
}