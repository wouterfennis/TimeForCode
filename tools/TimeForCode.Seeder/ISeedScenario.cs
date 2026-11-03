namespace TimeForCode.Seeder
{
    /// <summary>One named step of the seeding. It declares the personas it acts as, so persona selection is visible without running it.</summary>
    internal interface ISeedScenario
    {
        string Name { get; }

        IReadOnlyList<Persona> Personas { get; }

        Task RunAsync(SeedContext context);
    }
}