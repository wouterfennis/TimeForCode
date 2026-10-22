namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>Tells step definitions whether the running scenario talks to a live stack or to the in-process host.</summary>
    internal sealed record LiveScenario(bool IsLive);
}