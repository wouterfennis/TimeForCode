using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using TimeForCode.Reporting.Api.V1.Controllers;
using TimeForCode.Reporting.Api.V1.Rendering;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Api.Tests
{
    [TestClass]
    public class BadgeImageControllerTests
    {
        private const string Id = "507f1f77bcf86cd799439011";

        private readonly Mock<IMediator> _mediator = new();
        private readonly DefaultHttpContext _httpContext = new();

        private BadgeImageController CreateSut() => new(_mediator.Object, Options.Create(new BadgeImageOptions()))
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };

        private string CacheControl => _httpContext.Response.Headers.CacheControl.ToString();

        private void GivenContent(BadgeImageContent content)
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetProjectBadgeImageQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<BadgeImageContent>.Success(content));
            _mediator.Setup(m => m.Send(It.IsAny<GetOrganizationBadgeImageQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<BadgeImageContent>.Success(content));
            _mediator.Setup(m => m.Send(It.IsAny<GetContributorBadgeImageQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<BadgeImageContent>.Success(content));
        }

        private static ContentResult Svg(IActionResult result)
        {
            ContentResult content = result.Should().BeOfType<ContentResult>().Subject;
            content.ContentType.Should().Be("image/svg+xml");
            return content;
        }

        [TestMethod]
        public async Task GetProjectHoursReceivedBadge_SendsTheQueryAndReturnsTheImage()
        {
            GivenContent(new BadgeImageContent(BadgeImageState.HoursReceived, "120 hours"));

            IActionResult result = await CreateSut().GetProjectHoursReceivedBadge(Id);

            Svg(result).Content.Should().Be(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.HoursReceived, "120 hours")));
            _mediator.Verify(m => m.Send(It.Is<GetProjectBadgeImageQuery>(q => q.ProjectId == Id && q.Kind == ProjectBadgeKind.HoursReceived), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetProjectProgressBadge_SendsTheProgressQuery()
        {
            GivenContent(new BadgeImageContent(BadgeImageState.Progress, "45 / 120 hours (38%)"));

            IActionResult result = await CreateSut().GetProjectProgressBadge(Id);

            Svg(result).Content.Should().Contain("45 / 120 hours (38%)");
            _mediator.Verify(m => m.Send(It.Is<GetProjectBadgeImageQuery>(q => q.ProjectId == Id && q.Kind == ProjectBadgeKind.Progress), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetOrganizationBadge_SendsTheQueryAndReturnsTheImage()
        {
            GivenContent(new BadgeImageContent(BadgeImageState.Tier, "Gold"));

            IActionResult result = await CreateSut().GetOrganizationBadge(Id);

            Svg(result).Content.Should().Contain("Gold");
            _mediator.Verify(m => m.Send(It.Is<GetOrganizationBadgeImageQuery>(q => q.DonorOrganizationId == Id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetContributorBadge_SendsTheQueryAndReturnsTheImage()
        {
            GivenContent(new BadgeImageContent(BadgeImageState.Tier, "Silver"));

            IActionResult result = await CreateSut().GetContributorBadge(Id);

            Svg(result).Content.Should().Contain("Silver");
            _mediator.Verify(m => m.Send(It.Is<GetContributorBadgeImageQuery>(q => q.ContributorId == Id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetProjectHoursReceivedBadge_ApplicationFails_ReturnsTheUnavailableImageWithAShortCache()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetProjectBadgeImageQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<BadgeImageContent>.Failure("unexpected"));

            IActionResult result = await CreateSut().GetProjectHoursReceivedBadge(Id);

            Svg(result).Content.Should().Be(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Unavailable)));
            CacheControl.Should().Be("public, max-age=30");
        }

        [TestMethod]
        [DataRow(BadgeImageState.NoBadgeYet)]
        [DataRow(BadgeImageState.NotFound)]
        public async Task GetProjectHoursReceivedBadge_ValueOrNeutralImage_IsCachedForTheConfiguredTime(BadgeImageState state)
        {
            GivenContent(new BadgeImageContent(state));

            await CreateSut().GetProjectHoursReceivedBadge(Id);

            CacheControl.Should().Be("public, max-age=300");
        }

        [TestMethod]
        public async Task GetProjectHoursReceivedBadge_Value_IsCachedForTheConfiguredTime()
        {
            GivenContent(new BadgeImageContent(BadgeImageState.HoursReceived, "1 hour"));

            await CreateSut().GetProjectHoursReceivedBadge(Id);

            CacheControl.Should().Be("public, max-age=300");
        }

        [TestMethod]
        public async Task GetProjectHoursReceivedBadge_Unavailable_IsCachedBrieflyOnly()
        {
            GivenContent(new BadgeImageContent(BadgeImageState.Unavailable));

            await CreateSut().GetProjectHoursReceivedBadge(Id);

            CacheControl.Should().Be("public, max-age=30");
        }

        [TestMethod]
        public async Task GetProjectHoursReceivedBadge_CustomOptions_AreUsed()
        {
            GivenContent(new BadgeImageContent(BadgeImageState.Unavailable));
            var sut = new BadgeImageController(_mediator.Object, Options.Create(new BadgeImageOptions { CacheSeconds = 600, UnavailableCacheSeconds = 5 }))
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };

            await sut.GetProjectHoursReceivedBadge(Id);

            CacheControl.Should().Be("public, max-age=5");
        }

        [TestMethod]
        public void BadgeImageOptions_Defaults_AreFiveMinutesAndHalfAMinute()
        {
            var options = new BadgeImageOptions();

            options.CacheSeconds.Should().Be(300);
            options.UnavailableCacheSeconds.Should().Be(30);
        }

        [TestMethod]
        [DataRow(nameof(BadgeImageController.GetProjectHoursReceivedBadge))]
        [DataRow(nameof(BadgeImageController.GetProjectProgressBadge))]
        [DataRow(nameof(BadgeImageController.GetOrganizationBadge))]
        [DataRow(nameof(BadgeImageController.GetContributorBadge))]
        public void BadgeActions_AreAnonymousAndUseTheBadgeRateLimit(string action)
        {
            MethodInfo method = typeof(BadgeImageController).GetMethod(action)!;

            method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false).Should().NotBeEmpty();
            method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
                .Cast<EnableRateLimitingAttribute>().Should().ContainSingle().Which.PolicyName.Should().Be("badge");
        }
    }
}