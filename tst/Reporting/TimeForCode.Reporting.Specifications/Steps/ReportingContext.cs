using System.Net;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>State the step classes of one scenario share: the donor organization it works on and the last response.</summary>
    internal class ReportingContext
    {
        public string? DonorOrganizationId { get; set; }

        public string? ProjectId { get; set; }

        public HttpStatusCode? LastStatusCode { get; private set; }

        public string? LastContentType { get; private set; }

        public string? LastBody { get; private set; }

        /// <summary>Sends the request and keeps what the Then-steps assert on: the status, the content type and the body.</summary>
        public Task GetAsync(HttpClient client, string url) => SendAsync(() => client.GetAsync(new Uri(url, UriKind.Relative)));

        /// <summary>Runs the request and keeps what the Then-steps assert on, like <see cref="GetAsync"/> does for a GET.</summary>
        public async Task SendAsync(Func<Task<HttpResponseMessage>> send)
        {
            using HttpResponseMessage response = await send();
            LastStatusCode = response.StatusCode;
            LastContentType = response.Content.Headers.ContentType?.MediaType;
            LastBody = await response.Content.ReadAsStringAsync();
        }
    }
}