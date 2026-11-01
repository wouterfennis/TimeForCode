using TimeForCode.Authorization.Api.Client;
using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder
{
    internal sealed record SeedClients(IDonationClient Donation, IAuthClient Auth);

    /// <summary>Signs in personas and hands out API clients that act as them.</summary>
    internal interface ISeedSession
    {
        Task<SeedClients> SignInAsync(Persona persona);
    }
}