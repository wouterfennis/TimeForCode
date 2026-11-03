using AwesomeAssertions;
using System.Net;
using TimeForCode.Seeder;

namespace TimeForCode.Seeder.Tests
{
    [TestClass]
    public class StackProbeTests
    {
        private static readonly Uri Authorization = new("http://auth.test:8080");
        private static readonly Uri Donation = new("http://donation.test:8082");

        [TestMethod]
        public async Task CheckAsync_BothReachable_Succeeds()
        {
            using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

            Func<Task> act = () => StackProbe.CheckAsync(client, Authorization, Donation);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task CheckAsync_AnAnswerThatIsAnErrorStatus_StillCountsAsReachable()
        {
            using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));

            Func<Task> act = () => StackProbe.CheckAsync(client, Authorization, Donation);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task CheckAsync_ApiDoesNotAnswer_NamesTheApiAndTheLocalStartScript()
        {
            using var client = new HttpClient(new StubHandler(request =>
                request.RequestUri!.Host == "donation.test" ? throw new HttpRequestException("refused") : new HttpResponseMessage(HttpStatusCode.OK)));

            Func<Task> act = () => StackProbe.CheckAsync(client, Authorization, Donation);

            await act.Should().ThrowAsync<SeederException>().WithMessage("*http://donation.test:8082*start-local.ps1*");
        }

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(respond(request));
        }
    }
}