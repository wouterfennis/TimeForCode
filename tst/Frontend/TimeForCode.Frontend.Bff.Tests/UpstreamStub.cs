using System.Net;
using System.Text;

namespace TimeForCode.Frontend.Bff.Tests
{
    /// <summary>
    /// Stands in for the Authorization and Donation APIs: records what the BFF forwards and answers with <see cref="Respond"/>.
    /// </summary>
    public sealed class UpstreamStub : HttpMessageHandler
    {
        public List<ForwardedRequest> Requests { get; } = [];

        public Func<HttpResponseMessage> Respond { get; set; } = () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"projects\":[]}", Encoding.UTF8, "application/json"),
        };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new ForwardedRequest(
                request.Method,
                request.RequestUri!,
                request.Headers.ToDictionary(header => header.Key, header => string.Join("; ", header.Value), StringComparer.OrdinalIgnoreCase)));
            return Task.FromResult(Respond());
        }
    }

    public sealed record ForwardedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers);
}