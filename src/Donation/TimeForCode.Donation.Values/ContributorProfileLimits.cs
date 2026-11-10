namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Size limits of the contributor profile, shared by the domain, the commands and the API.
    /// </summary>
    public static class ContributorProfileLimits
    {
        /// <summary>The longest role, in characters.</summary>
        public const int MaxRoleLength = 100;

        /// <summary>The most skills a profile can list.</summary>
        public const int MaxSkills = 50;

        /// <summary>The longest single skill, in characters.</summary>
        public const int MaxSkillLength = 100;

        /// <summary>The most hours a contributor can offer.</summary>
        public const int MaxHoursAvailable = 10_000;
    }
}