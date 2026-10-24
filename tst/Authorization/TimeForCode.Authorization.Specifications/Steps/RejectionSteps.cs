using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Reqnroll;

namespace TimeForCode.Authorization.Specifications.Steps
{
    /// <summary>Status-only assertions on the last failed call; domain wording belongs in the scenario name.</summary>
    [Binding]
    internal class RejectionSteps
    {
        private readonly SessionContext _session;

        public RejectionSteps(SessionContext session)
        {
            _session = session;
        }

        [Then("The request is rejected as invalid")]
        public void ThenTheRequestIsRejectedAsInvalid() => AssertRejectedWith(StatusCodes.Status400BadRequest);

        [Then("The request is rejected as unauthorized")]
        public void ThenTheRequestIsRejectedAsUnauthorized() => AssertRejectedWith(StatusCodes.Status401Unauthorized);

        [Then("The request is rejected as not found")]
        public void ThenTheRequestIsRejectedAsNotFound() => AssertRejectedWith(StatusCodes.Status404NotFound);

        [Then("The request is rejected as a bad gateway")]
        public void ThenTheRequestIsRejectedAsABadGateway() => AssertRejectedWith(StatusCodes.Status502BadGateway);

        private void AssertRejectedWith(int expectedStatusCode)
        {
            _session.LastStatusCode.Should().NotBeNull($"the request should be rejected with status {expectedStatusCode}, but no failed call was captured");
            _session.LastStatusCode.Should().Be(expectedStatusCode, "the request should be rejected with status {0}", expectedStatusCode);
        }
    }
}