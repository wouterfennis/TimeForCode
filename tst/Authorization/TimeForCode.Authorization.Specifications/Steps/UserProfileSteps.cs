using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class UserProfileSteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private UserResponse? _response = null;
        private ApiException? _exception = null;

        public UserProfileSteps(IAuthClient authClient, IServiceProvider provider)
        {
            _authClient = authClient;
            _provider = provider;
        }

        [Given("The user does not have a profile on the time for code platform")]
        public void GivenTheUserDoesNotHaveAProfileOnTheTimeForCodePlatform()
        {
            var mockRepo = _provider.GetRequiredService<Mock<IAccountInformationRepository>>();
            mockRepo.Setup(x => x.GetByInternalIdAsync(It.IsAny<string>()))
                .ReturnsAsync((AccountInformation)null!);
        }

        [When("The user requests their profile from the time for code platform")]
        public async Task WhenTheUserRequestsTheirProfileFromTheTimeForCodePlatformAsync()
        {
            try
            {
                _response = await _authClient.UserAsync();
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }

        [Then("The profile is returned with name, login, avatar, email, company, bio, and location")]
        public void ThenTheProfileIsReturnedWithNameLoginAvatarEmailCompanyBioAndLocation()
        {
            _response.Should().NotBeNull();
            _response!.Name.Should().NotBeNullOrEmpty();
            _response!.Login.Should().NotBeNullOrEmpty();
            _response!.AvatarUrl.Should().NotBeNullOrEmpty();
            _response!.Email.Should().NotBeNull();
            _response!.Company.Should().NotBeNull();
            _response!.Bio.Should().NotBeNull();
            _response!.Location.Should().NotBeNull();
        }

        [Then("The user is informed their profile was not found")]
        public void ThenTheUserIsInformedTheirProfileWasNotFound()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }
    }
}