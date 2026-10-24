using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Reqnroll;
using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Status-only assertions on the failed API response of the last call; domain wording belongs in the scenario name.</summary>
    [Binding]
    internal class RejectionSteps
    {
        private readonly DonorOrganizationSteps _donorOrganizationSteps;
        private readonly ProjectSteps _projectSteps;

        public RejectionSteps(DonorOrganizationSteps donorOrganizationSteps, ProjectSteps projectSteps)
        {
            _donorOrganizationSteps = donorOrganizationSteps;
            _projectSteps = projectSteps;
        }

        [Then("The request is rejected as invalid")]
        public void ThenTheRequestIsRejectedAsInvalid() => AssertRejectedWith(StatusCodes.Status400BadRequest);

        [Then("The request is rejected as unauthorized")]
        public void ThenTheRequestIsRejectedAsUnauthorized() => AssertRejectedWith(StatusCodes.Status401Unauthorized);

        [Then("The request is rejected as forbidden")]
        public void ThenTheRequestIsRejectedAsForbidden() => AssertRejectedWith(StatusCodes.Status403Forbidden);

        [Then("The request is rejected as not found")]
        public void ThenTheRequestIsRejectedAsNotFound() => AssertRejectedWith(StatusCodes.Status404NotFound);

        [Then("The request is rejected as a conflict")]
        public void ThenTheRequestIsRejectedAsAConflict() => AssertRejectedWith(StatusCodes.Status409Conflict);

        private void AssertRejectedWith(int expectedStatusCode)
        {
            ApiException? exception = _donorOrganizationSteps.LastException ?? _projectSteps.LastException;

            exception.Should().NotBeNull($"the request should be rejected with status {expectedStatusCode}, but it succeeded");
            exception!.StatusCode.Should().Be(expectedStatusCode, "the request should be rejected with status {0}", expectedStatusCode);
        }
    }
}