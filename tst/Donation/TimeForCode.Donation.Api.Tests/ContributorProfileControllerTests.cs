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
    public class ContributorProfileControllerTests
    {
        private const string UserId = "user-1";
        private const string ContributorId = "507f1f77bcf86cd799439011";

        private readonly Mock<IMediator> _mediator = new();

        private ContributorProfileController CreateSut(bool withIdentity = true, bool admin = false)
        {
            var claims = new List<Claim>();
            if (withIdentity)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, UserId));
                claims.Add(new Claim("login", "octocat"));
                claims.Add(new Claim("avatar_url", "https://avatars.githubusercontent.com/u/1"));
            }
            if (admin)
            {
                claims.Add(new Claim("scope", "admin"));
            }

            return new ContributorProfileController(_mediator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
                }
            };
        }

        private static ContributorProfileDto Profile() => new()
        {
            Id = ContributorId,
            GithubLogin = "octocat",
            AvatarUrl = "https://avatars.githubusercontent.com/u/1",
            Role = "Backend developer",
            Skills = ["C#"],
            HoursAvailable = 10
        };

        private static UpdateContributorProfileRequest Request() => new() { Role = "Backend developer", Skills = ["C#"], HoursAvailable = 10 };

        private static Result<ContributorProfileResult> ProfileResult() =>
            Result<ContributorProfileResult>.Success(new ContributorProfileResult { Profile = Profile() });

        [TestMethod]
        public async Task GetMyContributorProfile_NoIdentity_ReturnsBadRequest()
        {
            var result = await CreateSut(withIdentity: false).GetMyContributorProfile();

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetMyContributorProfile_Found_ReturnsProfile()
        {
            _mediator.Setup(m => m.Send(It.Is<GetContributorProfileQuery>(q => q.CallerUserId == UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProfileResult());

            var result = await CreateSut().GetMyContributorProfile();

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ContributorProfileResponse>()
                .Which.GithubLogin.Should().Be("octocat");
        }

        [TestMethod]
        public async Task GetMyContributorProfile_NotFound_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetContributorProfileQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributorProfileResult>.NotFound("Contributor not found."));

            var result = await CreateSut().GetMyContributorProfile();

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [TestMethod]
        public async Task UpdateMyContributorProfile_NoLogin_ReturnsBadRequest()
        {
            var result = await CreateSut(withIdentity: false).UpdateMyContributorProfile(Request());

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task UpdateMyContributorProfile_ValidRequest_SendsOwnProfileCommand()
        {
            UpdateContributorProfileCommand? sent = null;
            _mediator.Setup(m => m.Send(It.IsAny<UpdateContributorProfileCommand>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest<Result<ContributorProfileResult>>, CancellationToken>((c, _) => sent = (UpdateContributorProfileCommand)c)
                .ReturnsAsync(ProfileResult());

            var result = await CreateSut().UpdateMyContributorProfile(Request());

            result.Should().BeOfType<OkObjectResult>();
            sent!.ContributorId.Should().BeNull();
            sent.IsAdministrator.Should().BeFalse();
            sent.CallerUserId.Should().Be(UserId);
        }

        [TestMethod]
        public async Task UpdateContributorProfile_Administrator_SendsAdministratorCommand()
        {
            UpdateContributorProfileCommand? sent = null;
            _mediator.Setup(m => m.Send(It.IsAny<UpdateContributorProfileCommand>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest<Result<ContributorProfileResult>>, CancellationToken>((c, _) => sent = (UpdateContributorProfileCommand)c)
                .ReturnsAsync(ProfileResult());

            await CreateSut(admin: true).UpdateContributorProfile(ContributorId, Request());

            sent!.IsAdministrator.Should().BeTrue();
            sent.ContributorId.Should().Be(ContributorId);
        }

        [TestMethod]
        public async Task UpdateContributorProfile_OtherContributor_ReturnsForbidden()
        {
            _mediator.Setup(m => m.Send(It.IsAny<UpdateContributorProfileCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributorProfileResult>.Forbidden("Not allowed."));

            var result = await CreateSut().UpdateContributorProfile(ContributorId, Request());

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [TestMethod]
        public async Task GetContributorProfile_Administrator_PassesAdministratorFlag()
        {
            GetContributorProfileQuery? sent = null;
            _mediator.Setup(m => m.Send(It.IsAny<GetContributorProfileQuery>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest<Result<ContributorProfileResult>>, CancellationToken>((q, _) => sent = (GetContributorProfileQuery)q)
                .ReturnsAsync(ProfileResult());

            await CreateSut(admin: true).GetContributorProfile(ContributorId);

            sent!.IsAdministrator.Should().BeTrue();
            sent.ContributorId.Should().Be(ContributorId);
        }

        [TestMethod]
        public async Task GetDonorOrganizationContributors_Found_ReturnsContributors()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetDonorOrganizationContributorsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationContributorsResult>.Success(new GetDonorOrganizationContributorsResult { Contributors = [Profile()] }));

            var result = await CreateSut(admin: true).GetDonorOrganizationContributors(ContributorId);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<GetDonorOrganizationContributorsResponse>()
                .Which.Contributors.Should().ContainSingle();
        }

        [TestMethod]
        public async Task GetDonorOrganizationContributors_UnknownOrganization_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetDonorOrganizationContributorsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationContributorsResult>.NotFound("Donor organization not found."));

            var result = await CreateSut(admin: true).GetDonorOrganizationContributors(ContributorId);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }
    }
}