using System.Net.Http.Json;

namespace TimeForCode.Seeder
{
    internal sealed class ReportingClient : IReportingClient
    {
        private readonly HttpClient _httpClient;

        public ReportingClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task DefineBadgeTierAsync(string recipientType, string name, int thresholdHours)
        {
            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                new Uri($"api/v1/reporting/badge-tier/{Uri.EscapeDataString(recipientType)}/{Uri.EscapeDataString(name)}", UriKind.Relative),
                new { thresholdHours });

            if (!response.IsSuccessStatusCode)
            {
                throw new SeederException($"Defining the {recipientType} badge tier '{name}' failed with status {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}