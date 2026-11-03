namespace TimeForCode.Seeder
{
    /// <summary>The fixed set of personas; names are lower case because the mock identity provider lower-cases them into the GitHub login.</summary>
    internal static class Personas
    {
        public static readonly Persona Alice = new("alice-contributor", PersonaRole.Contributor);
        public static readonly Persona Bob = new("bob-contributor", PersonaRole.Contributor);
        public static readonly Persona Carol = new("carol-contributor", PersonaRole.Contributor);
        public static readonly Persona Maya = new("maya-maintainer", PersonaRole.Maintainer);
        public static readonly Persona Marco = new("marco-maintainer", PersonaRole.Maintainer);
        public static readonly Persona Dana = new("dana-donor", PersonaRole.DonorMember);
        public static readonly Persona Dirk = new("dirk-donor", PersonaRole.DonorMember);

        /// <summary>Signs in through the test-only admin token endpoint, not through the mock identity provider.</summary>
        public static readonly Persona Admin = new("admin", PersonaRole.Admin);

        public static IReadOnlyList<Persona> All { get; } = [Alice, Bob, Carol, Maya, Marco, Dana, Dirk, Admin];
    }
}