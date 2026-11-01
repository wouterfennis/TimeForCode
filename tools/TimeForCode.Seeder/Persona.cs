namespace TimeForCode.Seeder
{
    internal enum PersonaRole
    {
        Contributor,
        Maintainer,
        DonorMember,
        Admin
    }

    /// <summary>A named user of the seeded stack. The name is also the mock identity provider persona, so it fixes the user id and GitHub login.</summary>
    internal sealed record Persona(string Name, PersonaRole Role);
}