using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Reqnroll;
using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        [When("The user requests the project with the identifier {string}")]
        public async Task WhenTheUserRequestsTheProjectWithTheIdentifierAsync(string identifier)
        {
            try
            {
                _getProjectByIdResult = await _donationClient.GetProjectByIdAsync(identifier);
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

        [Then("The user is informed the project identifier is invalid")]
        public void ThenTheUserIsInformedTheProjectIdentifierIsInvalid()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }
    }
}