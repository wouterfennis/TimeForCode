using System.Net.Http.Headers;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// A delegating handler that adds a bearer token to every outgoing request.
    /// The token can be set or cleared between test steps.
    /// </summary>
    internal class BearerTokenHandler : DelegatingHandler
    {
        public string? Token { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
