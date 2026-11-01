namespace TimeForCode.Seeder
{
    /// <summary>What a running scenario may use: the personas it declared and the ids earlier scenarios remembered.</summary>
    internal sealed class SeedContext
    {
        private readonly string _scenarioName;
        private readonly IReadOnlyList<Persona> _personas;
        private readonly ISeedSession _session;
        private readonly Dictionary<string, string> _ids;

        public SeedContext(string scenarioName, IReadOnlyList<Persona> personas, ISeedSession session, Dictionary<string, string> ids)
        {
            _scenarioName = scenarioName;
            _personas = personas;
            _session = session;
            _ids = ids;
        }

        public Task<SeedClients> ClientsFor(Persona persona)
        {
            if (!_personas.Contains(persona))
            {
                throw new SeederException($"Scenario '{_scenarioName}' signed in as '{persona.Name}' without declaring it.");
            }

            return _session.SignInAsync(persona);
        }

        public void Remember(string key, string id) => _ids[key] = id;

        public string IdOf(string key) =>
            _ids.TryGetValue(key, out string? id)
                ? id
                : throw new SeederException($"Scenario '{_scenarioName}' needs '{key}', which no earlier scenario created.");
    }
}