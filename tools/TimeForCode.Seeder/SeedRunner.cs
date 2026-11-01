namespace TimeForCode.Seeder
{
    internal sealed class SeedRunner
    {
        private readonly ISeedSession _session;
        private readonly Action<string> _log;

        public SeedRunner(ISeedSession session, Action<string> log)
        {
            _session = session;
            _log = log;
        }

        public async Task RunAsync(IEnumerable<SeedScenario> scenarios)
        {
            var ids = new Dictionary<string, string>();

            foreach (SeedScenario scenario in scenarios)
            {
                _log($"Seeding {scenario.Name}");
                var context = new SeedContext(scenario.Name, scenario.Personas, _session, ids);

                try
                {
                    await scenario.RunAsync(context);
                }
                catch (SeederException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new SeederException($"Scenario '{scenario.Name}' failed: {exception.Message}", exception);
                }
            }
        }
    }
}