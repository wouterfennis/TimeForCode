using RichardSzalay.MockHttp;
using TimeForCode.Authorization.Api.Client;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    /// <summary>
    /// What several running instances of the time for code platform share within one scenario:
    /// the login state store, the external platform and the clock.
    /// </summary>
    public class ScenarioInfrastructure : IDisposable
    {
        private readonly List<string> _accessTokenRequests = [];
        private TimeForCodeWebApplicationFactory? _otherInstance;

        public InMemoryStateRepository States { get; } = new();

        public MockHttpMessageHandler MockHttp { get; } = new();

        public AdjustableTimeProvider Time { get; } = new();

        /// <summary>
        /// The client of another or restarted instance; null while the scenario only uses the first instance.
        /// </summary>
        internal IAuthClient? CallbackClient { get; set; }

        public IReadOnlyList<string> AccessTokenRequests => _accessTokenRequests;

        public void RecordAccessTokenRequest(string body) => _accessTokenRequests.Add(body);

        internal TimeForCodeWebApplicationFactory StartOtherInstance()
        {
            _otherInstance ??= new TimeForCodeWebApplicationFactory(this);
            return _otherInstance;
        }

        public void Dispose()
        {
            _otherInstance?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}