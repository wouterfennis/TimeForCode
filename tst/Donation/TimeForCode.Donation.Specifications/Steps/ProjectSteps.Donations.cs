using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Donation.Specifications.TestBuilder;
using DomainStatus = TimeForCode.Donation.Values.DonationStatus;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        private const string UnknownDonationId = "5f43a0e74b12c84f1b0000ff";

        private DonationResponse? _donationResult;
        private GetDonationsResponse? _getDonationsResult;
        private DonorOrganizationResponse? _organizationResult;
        private string? _trackedDonationId;

        private InMemoryDonationRepository Donations => _provider.GetRequiredService<InMemoryDonationRepository>();

        [Given("^The user has an? (pending|active|completed|cancelled) donation$")]
        public void GivenTheUserHasADonation(string state)
        {
            _trackedDonationId = AddDonation(state, Constants.TestUserId, null, null, null);
        }

        [Given("^The user has an? (pending|active|completed|cancelled) donation of (\\d+) hours$")]
        public void GivenTheUserHasADonationOfHours(string state, string committed)
        {
            _trackedDonationId = AddDonation(state, Constants.TestUserId, committed, null, null);
        }

        [Given("^The user has an? (pending|active|completed|cancelled) donation of (\\d+) hours with (\\d+) hours logged$")]
        public void GivenTheUserHasADonationOfHoursWithHoursLogged(string state, string committed, string logged)
        {
            _trackedDonationId = AddDonation(state, Constants.TestUserId, committed, logged, null);
        }

        [Given("^There is an? (pending|active|completed|cancelled) donation of another user$")]
        public void GivenThereIsADonationOfAnotherUser(string state)
        {
            _trackedDonationId = AddDonation(state, "another-user-id", null, null, null);
        }

        [Given("The donor organization has an active donation of {int} hours")]
        public void GivenTheDonorOrganizationHasAnActiveDonation(int hours)
        {
            var organization = DonorOrganizationBuilder.BuildExisting();
            _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>()
                .Setup(x => x.GetByIdAsync(organization.Id.ToString()))
                .ReturnsAsync(organization);

            _trackedDonationId = AddDonation("active", Constants.TestUserId, hours.ToString(), null, organization.Id);
        }

        [Given("There is no donation with the given identifier")]
        public void GivenThereIsNoDonationWithTheGivenIdentifier()
        {
            _trackedDonationId = UnknownDonationId;
        }

        [Given("There is a published project with {int} donations")]
        public void GivenThereIsAPublishedProjectWithDonations(int count)
        {
            GivenThereIsAPublishedProjectOnTheTimeForCodePlatform();
            for (var i = 0; i < count; i++)
            {
                AddDonation("pending", "another-user-id", null, null, null);
            }
        }

        [When("The user pledges {int} hours to the project as an individual")]
        public Task WhenTheUserPledgesHoursAsAnIndividualAsync(int hours)
        {
            return PledgeAsync(hours, asIndividual: true, organizationId: null);
        }

        [When("The user pledges {int} hours to the project for the donor organization")]
        public Task WhenTheUserPledgesHoursForTheDonorOrganizationAsync(int hours)
        {
            return PledgeAsync(hours, asIndividual: false, organizationId: Constants.TestDonorOrganizationId);
        }

        [When("The user pledges {int} hours to the project as an individual and for the donor organization")]
        public Task WhenTheUserPledgesHoursAsAnIndividualAndForTheDonorOrganizationAsync(int hours)
        {
            return PledgeAsync(hours, asIndividual: true, organizationId: Constants.TestDonorOrganizationId);
        }

        [When("The user pledges {int} hours to the project without a donor")]
        public Task WhenTheUserPledgesHoursWithoutADonorAsync(int hours)
        {
            return PledgeAsync(hours, asIndividual: false, organizationId: null);
        }

        [When("^The user (activates|cancels) the donation$")]
        public Task WhenTheUserChangesTheDonationAsync(string action)
        {
            var id = _trackedDonationId ?? UnknownDonationId;
            return CaptureAsync(async () => _donationResult = action == "activates"
                ? await _donationClient.ActivateDonationAsync(id)
                : await _donationClient.CancelDonationAsync(id));
        }

        [When("The user requests their donations")]
        public Task WhenTheUserRequestsTheirDonationsAsync()
        {
            return CaptureAsync(async () => _getDonationsResult = await _donationClient.GetMyDonationsAsync());
        }

        [When("The user requests the donations of the project")]
        public Task WhenTheUserRequestsTheDonationsOfTheProjectAsync()
        {
            return CaptureAsync(async () => _getDonationsResult = await _donationClient.GetProjectDonationsAsync(_registeredProjectId ?? Constants.TestProjectId));
        }

        [Then("The donation is recorded on the time for code platform")]
        public void ThenTheDonationIsRecordedOnTheTimeForCodePlatform()
        {
            _exception.Should().BeNull();
            _donationResult.Should().NotBeNull();
            Donations.Donations.Should().Contain(d => d.Id.ToString() == _donationResult!.Id);
        }

        [Then("^The donation is (pending|active|completed|cancelled)$")]
        public void ThenTheDonationIsInState(string state)
        {
            _exception.Should().BeNull();
            StoredDonation().Status.Should().Be(ParseState(state));
        }

        [Then("The donation shows {int} hours committed and {int} hours logged")]
        public void ThenTheDonationShowsHoursCommittedAndLogged(int committed, int logged)
        {
            var (shownCommitted, shownLogged, _) = ShownDonation();

            shownCommitted.Should().Be(committed);
            shownLogged.Should().Be(logged);
        }

        [Then("The donation shows {int} hours logged")]
        public void ThenTheDonationShowsHoursLogged(int logged)
        {
            ShownDonation().Logged.Should().Be(logged);
        }

        [Then("The donation shows a progress of {int} percent")]
        public void ThenTheDonationShowsAProgressOfPercent(int percentage)
        {
            ShownDonation().Progress.Should().Be(percentage);
        }

        [Then("The donor organization shows {int} hours spent")]
        public async Task ThenTheDonorOrganizationShowsHoursSpentAsync(int hours)
        {
            (await RequestOrganizationAsync()).HoursSpent.Should().Be(hours);
        }

        [Then("The donor organization shows {int} hours available")]
        public async Task ThenTheDonorOrganizationShowsHoursAvailableAsync(int hours)
        {
            (await RequestOrganizationAsync()).HoursAvailable.Should().Be(hours);
        }

        [Then("The donations of the user are returned")]
        public void ThenTheDonationsOfTheUserAreReturned()
        {
            _exception.Should().BeNull();
            _getDonationsResult.Should().NotBeNull();
            _getDonationsResult!.Donations.Should().ContainSingle().Which.Id.Should().Be(_trackedDonationId);
        }

        [Then("An empty list of donations is returned")]
        public void ThenAnEmptyListOfDonationsIsReturned()
        {
            _exception.Should().BeNull();
            _getDonationsResult.Should().NotBeNull();
            _getDonationsResult!.Donations.Should().BeEmpty();
        }

        [Then("{int} donations are returned")]
        public void ThenDonationsAreReturned(int count)
        {
            _exception.Should().BeNull();
            _getDonationsResult.Should().NotBeNull();
            _getDonationsResult!.Donations.Should().HaveCount(count);
        }

        [Then("Each donation shows its status, hours committed, hours logged and progress")]
        public void ThenEachDonationShowsItsStatusAndProgress()
        {
            _getDonationsResult.Should().NotBeNull();
            _getDonationsResult!.Donations.Should().OnlyContain(d => d.HoursCommitted > 0 && d.HoursLogged >= 0 && d.ProgressPercentage >= 0);
        }

        private Task PledgeAsync(int hours, bool asIndividual, string? organizationId)
        {
            return CaptureAsync(async () => _donationResult = await _donationClient.PledgeDonationAsync(
                _registeredProjectId ?? Constants.TestProjectId,
                new PledgeDonationRequest { Hours = hours, AsIndividual = asIndividual, DonorOrganizationId = organizationId }));
        }

        private string AddDonation(string state, string pledgedByUserId, string? committed, string? logged, ObjectId? organizationId)
        {
            EnsureActiveProjectRegistered();

            var donation = DonationBuilder.Build(
                ObjectIdOf(_registeredProjectId ?? Constants.TestProjectId),
                ParseState(state),
                pledgedByUserId,
                string.IsNullOrEmpty(committed) ? 20 : int.Parse(committed),
                string.IsNullOrEmpty(logged) ? 0 : int.Parse(logged),
                organizationId);
            Donations.CreateAsync(donation);

            return donation.Id.ToString();
        }

        private void EnsureActiveProjectRegistered()
        {
            if (_registeredProjectId != null || _lifecycleProject != null)
            {
                return;
            }

            GivenThereIsAPublishedProjectOnTheTimeForCodePlatform();
        }

        private DonationEntity StoredDonation()
        {
            var id = _trackedDonationId ?? _donationResult?.Id;
            return Donations.Donations.Single(d => d.Id.ToString() == id);
        }

        private (int Committed, int Logged, int Progress) ShownDonation()
        {
            _exception.Should().BeNull();

            if (_getDonationsResult != null)
            {
                var shown = _getDonationsResult.Donations.Should().ContainSingle().Subject;
                return (shown.HoursCommitted, shown.HoursLogged, shown.ProgressPercentage);
            }

            if (_donationResult != null && _trackedDonationId == null)
            {
                return (_donationResult.HoursCommitted, _donationResult.HoursLogged, _donationResult.ProgressPercentage);
            }

            var stored = StoredDonation();
            return (stored.HoursCommitted, stored.HoursLogged, stored.ProgressPercentage);
        }

        private async Task<DonorOrganizationResponse> RequestOrganizationAsync()
        {
            _organizationResult ??= await _donationClient.GetDonorOrganizationByIdAsync(Constants.TestDonorOrganizationId);
            return _organizationResult;
        }

        private static DomainStatus ParseState(string state)
        {
            return Enum.Parse<DomainStatus>(state, ignoreCase: true);
        }
    }
}