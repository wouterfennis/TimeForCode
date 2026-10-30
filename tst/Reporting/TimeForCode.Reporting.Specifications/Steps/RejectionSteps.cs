using AwesomeAssertions;
using Reqnroll;
using System.Net;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>Status-only assertions on the last response; domain wording belongs in the scenario name.</summary>
    [Binding]
    internal class RejectionSteps
    {
        private readonly ReportingContext _context;

        public RejectionSteps(ReportingContext context)
        {
            _context = context;
        }

        [Then("The request is rejected as unauthorized")]
        public void ThenTheRequestIsRejectedAsUnauthorized() => AssertRejectedWith(HttpStatusCode.Unauthorized);

        [Then("The request is rejected as forbidden")]
        public void ThenTheRequestIsRejectedAsForbidden() => AssertRejectedWith(HttpStatusCode.Forbidden);

        [Then("The request is rejected as not found")]
        public void ThenTheRequestIsRejectedAsNotFound() => AssertRejectedWith(HttpStatusCode.NotFound);

        private void AssertRejectedWith(HttpStatusCode expected)
        {
            _context.LastStatusCode.Should().Be(expected, "the request should be rejected with status {0}", expected);
        }
    }
}