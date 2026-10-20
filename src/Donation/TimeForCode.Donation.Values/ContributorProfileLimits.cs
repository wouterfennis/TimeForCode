namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Size limits of the contributor profile, shared by the domain, the commands and the API.
    /// </summary>
    public static class ContributorProfileLimits
    {
        public const int MaxRoleLength = 100;
        public const int MaxSkills = 50;
        public const int MaxSkillLength = 100;
        public const int MaxHoursAvailable = 10_000;
    }
}