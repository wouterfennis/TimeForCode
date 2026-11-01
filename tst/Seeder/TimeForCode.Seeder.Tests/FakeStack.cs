using Moq;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Seeder;

namespace TimeForCode.Seeder.Tests
{
    /// <summary>An <see cref="ISeedSession"/> whose clients record every call as "persona:operation:argument" and invent ids.</summary>
    internal sealed class FakeStack : ISeedSession
    {
        private readonly Dictionary<string, SeedClients> _clients = [];
        private int _counter;

        public List<string> Calls { get; } = [];

        public Task<SeedClients> SignInAsync(Persona persona)
        {
            if (!_clients.TryGetValue(persona.Name, out SeedClients? clients))
            {
                clients = new SeedClients(CreateDonationClient(persona.Name), CreateAuthClient(persona.Name));
                _clients[persona.Name] = clients;
            }

            return Task.FromResult(clients);
        }

        public async Task RunAsync(params SeedScenario[] scenarios)
        {
            await new SeedRunner(this, _ => { }).RunAsync(scenarios);
        }

        private string NextId(string prefix) => $"{prefix}-{++_counter}";

        private IDonationClient CreateDonationClient(string persona)
        {
            var client = new Mock<IDonationClient>();

            client.Setup(c => c.RegisterProjectAsync(It.IsAny<RegisterProjectRequest>()))
                .ReturnsAsync(() => new RegisterProjectResult { ProjectId = NextId("project") })
                .Callback((RegisterProjectRequest r) => Calls.Add($"{persona}:RegisterProject:{r.GithubRepositoryUrl}"));
            client.Setup(c => c.SubmitProjectForReviewAsync(It.IsAny<string>()))
                .Callback((string id) => Calls.Add($"{persona}:Submit:{id}"));
            client.Setup(c => c.ApproveProjectAsync(It.IsAny<string>()))
                .Callback((string id) => Calls.Add($"{persona}:Approve:{id}"));
            client.Setup(c => c.RequestProjectChangesAsync(It.IsAny<string>(), It.IsAny<RequestProjectChangesRequest>()))
                .Callback((string id, RequestProjectChangesRequest r) => Calls.Add($"{persona}:RequestChanges:{id}:{r.Reason}"));
            client.Setup(c => c.ArchiveProjectAsync(It.IsAny<string>()))
                .Callback((string id) => Calls.Add($"{persona}:Archive:{id}"));
            client.Setup(c => c.CreateDonorOrganizationAsync(It.IsAny<CreateDonorOrganizationRequest>()))
                .ReturnsAsync(() => new DonorOrganizationResponse { Id = NextId("organization") })
                .Callback((CreateDonorOrganizationRequest r) => Calls.Add($"{persona}:CreateOrganization:{r.Name}"));
            client.Setup(c => c.UpdateMyContributorProfileAsync(It.IsAny<UpdateContributorProfileRequest>()))
                .Callback((UpdateContributorProfileRequest r) => Calls.Add($"{persona}:UpdateProfile:{r.DonorOrganizationId}"));
            client.Setup(c => c.PledgeDonationAsync(It.IsAny<string>(), It.IsAny<PledgeDonationRequest>()))
                .ReturnsAsync(() => new DonationResponse { Id = NextId("donation") })
                .Callback((string id, PledgeDonationRequest r) => Calls.Add($"{persona}:Pledge:{id}:{r.Hours}"));
            client.Setup(c => c.ActivateDonationAsync(It.IsAny<string>()))
                .Callback((string id) => Calls.Add($"{persona}:Activate:{id}"));
            client.Setup(c => c.CancelDonationAsync(It.IsAny<string>()))
                .Callback((string id) => Calls.Add($"{persona}:CancelDonation:{id}"));
            client.Setup(c => c.AddContributionAsync(It.IsAny<string>(), It.IsAny<AddContributionRequest>()))
                .ReturnsAsync(() => new ContributionResponse { Id = NextId("contribution") })
                .Callback((string id, AddContributionRequest r) => Calls.Add($"{persona}:AddContribution:{id}:{r.Hours}"));
            client.Setup(c => c.CorrectContributionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CorrectContributionRequest>()))
                .Callback((string id, string contribution, CorrectContributionRequest r) => Calls.Add($"{persona}:CorrectContribution:{contribution}:{r.Hours}"));
            client.Setup(c => c.DeleteContributionAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string id, string contribution) => Calls.Add($"{persona}:DeleteContribution:{contribution}"));

            return client.Object;
        }

        private IAuthClient CreateAuthClient(string persona)
        {
            var client = new Mock<IAuthClient>();
            client.Setup(c => c.DeleteAccountAsync()).Callback(() => Calls.Add($"{persona}:DeleteAccount"));
            return client.Object;
        }
    }
}