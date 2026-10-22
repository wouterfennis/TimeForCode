using FluentAssertions;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Specifications.Mocking;

namespace TimeForCode.Donation.Specifications.Steps
{
    [Binding]
    internal class LiveDonorOrganizationSteps
    {
        private readonly IDonationClient _donationClient;

        private DonorOrganizationResponse? _registered;

        public LiveDonorOrganizationSteps(IDonationClient donationClient)
        {
            _donationClient = donationClient;
        }

        [When("The user registers a donor organization with a unique name")]
        public async Task WhenTheUserRegistersADonorOrganizationWithAUniqueNameAsync()
        {
            _registered = await _donationClient.CreateDonorOrganizationAsync(new CreateDonorOrganizationRequest
            {
                Name = $"Live Organization {Guid.NewGuid():N}"
            });
        }

        [Then("The donor organization can be retrieved on the live platform")]
        public async Task ThenTheDonorOrganizationCanBeRetrievedOnTheLivePlatformAsync()
        {
            var retrieved = await _donationClient.GetDonorOrganizationByIdAsync(_registered!.Id);

            retrieved.Name.Should().Be(_registered.Name);
        }

        [AfterScenario(LiveHost.LiveOnlyTag)]
        public async Task DeleteRegisteredDonorOrganizationAsync()
        {
            if (_registered is not null)
            {
                await _donationClient.DeleteDonorOrganizationAsync(_registered.Id);
            }
        }
    }
}