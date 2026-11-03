using System.Net.Http.Headers;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Shared.Live;

namespace TimeForCode.Seeder
{
    /// <summary>Signs personas in against a running stack and caches the clients, so a persona signs in once per run.</summary>
    internal sealed class LiveSeedSession : ISeedSession
    {
        private readonly SeederOptions _options;
        private readonly Dictionary<string, SeedClients> _clients = [];

        public LiveSeedSession(SeederOptions options)
        {
            _options = options;
        }

        public async Task<SeedClients> SignInAsync(Persona persona)
        {
            if (_clients.TryGetValue(persona.Name, out SeedClients? known))
            {
                return known;
            }

            string token = await AcquireTokenAsync(persona);
            var clients = new SeedClients(
                new DonationClient(CreateHttpClient(_options.DonationUrl, token)),
                new AuthClient(CreateHttpClient(_options.AuthorizationUrl, token)));
            _clients[persona.Name] = clients;
            return clients;
        }

        private Task<string> AcquireTokenAsync(Persona persona)
        {
            return persona.Role == PersonaRole.Admin
                ? LiveAuthentication.RequestTestAdminTokenAsync(_options.AuthorizationUrl, _options.AdminSecret)
                : LiveAuthentication.SignInAsync(_options.AuthorizationUrl, _options.ClientRedirectUri, persona.Name);
        }

        private static HttpClient CreateHttpClient(Uri baseAddress, string token)
        {
            var client = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = baseAddress };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}