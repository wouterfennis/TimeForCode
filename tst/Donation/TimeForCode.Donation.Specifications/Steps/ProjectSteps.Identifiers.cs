using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Reqnroll;
using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        [When("^The user requests the (project|contributions of the project) with the identifier \"(.*)\"$")]
        public Task WhenTheUserRequestsTheProjectWithTheIdentifierAsync(string subject, string identifier)
        {
            return subject == "project"
                ? CaptureAsync(async () => _getProjectByIdResult = await _donationClient.GetProjectByIdAsync(identifier))
                : GetContributionsAsync(identifier);
        }

        [Then("The user is informed the project identifier is invalid")]
        [Then("The user is informed the GitHub reference is invalid")]
        [Then("The user is informed the hours are invalid")]
        public void ThenTheUserIsInformedTheRequestIsInvalid()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        /// <summary>
        /// Runs an API call and keeps a failed response in <c>_exception</c> for the Then-steps.
        /// </summary>
        private async Task CaptureAsync(Func<Task> apiCall)
        {
            try
            {
                await apiCall();
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
    }
}