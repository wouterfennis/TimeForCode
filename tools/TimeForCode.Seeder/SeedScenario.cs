namespace TimeForCode.Seeder
{
    /// <summary>One named step of the seeding. It declares the personas it acts as, so persona selection is visible without running it.</summary>
    internal abstract class SeedScenario
    {
        public abstract string Name { get; }

        public abstract IReadOnlyList<Persona> Personas { get; }

        public abstract Task RunAsync(SeedContext context);
    }
}