using AwesomeAssertions;
using System.Net;
using System.Text;

namespace TimeForCode.Seeder.Tests
{
    [TestClass]
    public class ReportingClientTests
    {
        [TestMethod]
        public async Task DefineBadgeTierAsync_Succeeds_PutsTheThresholdOnTheEscapedTierRoute()
        {
            var handler = new RecordingHandler(HttpStatusCode.NoContent);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://reporting.test:8083") };
            var client = new ReportingClient(httpClient);

            await client.DefineBadgeTierAsync("Organization", "Gold Tier", 100);

            handler.Method.Should().Be(HttpMethod.Put);
            handler.RequestUri.Should().Be("http://reporting.test:8083/api/v1/reporting/badge-tier/Organization/Gold%20Tier");
            handler.Body.Should().Be("{\"thresholdHours\":100}");
        }

        [TestMethod]
        public async Task DefineBadgeTierAsync_IsRejected_ThrowsSeederExceptionWithTheStatusAndTheBody()
        {
            var handler = new RecordingHandler(HttpStatusCode.Forbidden, "no admin scope");
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://reporting.test:8083") };
            var client = new ReportingClient(httpClient);

            Func<Task> act = () => client.DefineBadgeTierAsync("Contributor", "Bronze", 10);

            await act.Should().ThrowAsync<SeederException>()
                .WithMessage("*Contributor badge tier 'Bronze'*403*no admin scope*");
        }

        private sealed class RecordingHandler(HttpStatusCode status, string responseBody = "") : HttpMessageHandler
        {
            public HttpMethod? Method { get; private set; }

            public string? RequestUri { get; private set; }

            public string? Body { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Method = request.Method;
                RequestUri = request.RequestUri!.AbsoluteUri;
                Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(status) { Content = new StringContent(responseBody, Encoding.UTF8) };
            }
        }
    }
}