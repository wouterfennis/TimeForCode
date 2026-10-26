using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using System.Net.Http.Json;
using System.Text.Json;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.TestBuilder;

namespace TimeForCode.Donation.Specifications.Steps
{
    [Binding]
    internal class DonorOrganizationSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly IServiceProvider _provider;
        private readonly HttpClient _httpClient;
        private readonly DonationContext _context;

        private GetDonorOrganizationsResponse? _getOrganizationsResult;
        private DonorOrganizationResponse? _getOrganizationByIdResult;
        private DonorOrganizationResponse? _createResult;
        private DonorOrganizationResponse? _updateResult;
        private string? _registeredOrganizationId;

        public DonorOrganizationSteps(IDonationClient donationClient, IServiceProvider provider, HttpClient httpClient, DonationContext context)
        {
            _donationClient = donationClient;
            _provider = provider;
            _httpClient = httpClient;
            _context = context;
        }

        [Given("There are donor organizations on the time for code platform")]
        public void GivenThereAreDonorOrganizationsOnTheTimeForCodePlatform()
        {
            var org = DonorOrganizationBuilder.BuildExisting();
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((IReadOnlyList<DonorOrganization>)new List<DonorOrganization> { org }, 1));
        }

        [Given("There is a donor organization on the time for code platform")]
        public void GivenThereIsADonorOrganizationOnTheTimeForCodePlatform()
        {
            var org = DonorOrganizationBuilder.BuildExisting();
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.GetByIdAsync(org.Id.ToString()))
                .ReturnsAsync(org);
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((IReadOnlyList<DonorOrganization>)new List<DonorOrganization> { org }, 1));

            _registeredOrganizationId = org.Id.ToString();
            _context.DonorOrganizationId = _registeredOrganizationId;
        }

        [Given("There is no donor organization with the given identifier")]
        public void GivenThereIsNoDonorOrganizationWithTheGivenIdentifier()
        {
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((DonorOrganization?)null);
            _context.DonorOrganizationId = ObjectId.GenerateNewId().ToString();
        }

        [Given("There is already a donor organization with the same name on the time for code platform")]
        public void GivenThereIsAlreadyADonorOrganizationWithTheSameNameOnTheTimeForCodePlatform()
        {
            var conflictingOrg = DonorOrganizationBuilder.BuildExisting();
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(conflictingOrg);
        }

        [When("The user requests the list of donor organizations( without an account)")]
        public async Task WhenTheUserRequestsTheListOfDonorOrganizationsAsync()
        {
            try
            {
                _getOrganizationsResult = await _donationClient.GetDonorOrganizationsAsync(null, null);
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user requests page {int} of the donor organizations with {int} donor organizations per page")]
        public async Task WhenTheUserRequestsPageOfTheDonorOrganizationsAsync(int pageNumber, int pageSize)
        {
            try
            {
                _getOrganizationsResult = await _donationClient.GetDonorOrganizationsAsync(pageNumber, pageSize);
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user requests the donor organization with the identifier {string}")]
        public async Task WhenTheUserRequestsTheDonorOrganizationWithTheIdentifierAsync(string identifier)
        {
            try
            {
                _getOrganizationByIdResult = await _donationClient.GetDonorOrganizationByIdAsync(identifier);
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user requests the donor organization details")]
        public async Task WhenTheUserRequestsTheDonorOrganizationDetailsAsync()
        {
            var id = _registeredOrganizationId ?? Constants.TestDonorOrganizationId;
            try
            {
                _getOrganizationByIdResult = await _donationClient.GetDonorOrganizationByIdAsync(id);
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user registers a donor organization with a valid name")]
        public async Task WhenTheUserRegistersADonorOrganizationWithAValidNameAsync()
        {
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.CreateAsync(It.IsAny<DonorOrganization>()))
                .Returns(Task.CompletedTask);

            try
            {
                _createResult = await _donationClient.CreateDonorOrganizationAsync(new CreateDonorOrganizationRequest
                {
                    Name = "Acme Corp"
                });
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user registers a donor organization with the name {string}")]
        public Task WhenTheUserRegistersADonorOrganizationWithTheNameAsync(string name)
        {
            return RegisterAsync(new CreateDonorOrganizationRequest { Name = name });
        }

        [When("The user registers a donor organization with the contact email {string} and the website {string}")]
        public async Task WhenTheUserRegistersADonorOrganizationWithContactDetailsAsync(string email, string website)
        {
            // Sent as raw JSON: the generated client types the website as a Uri and cannot send invalid values.
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.CreateAsync(It.IsAny<DonorOrganization>()))
                .Returns(Task.CompletedTask);

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/donororganization",
                new { name = "Acme Corp", contactEmail = email, website });
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _createResult = JsonSerializer.Deserialize<DonorOrganizationResponse>(body, JsonSerializerOptions.Web);
                return;
            }

            _context.LastException = new ApiException(
                "The donor organization request was rejected.",
                (int)response.StatusCode,
                body,
                new Dictionary<string, IEnumerable<string>>(),
                null);
        }

        private async Task RegisterAsync(CreateDonorOrganizationRequest request)
        {
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.CreateAsync(It.IsAny<DonorOrganization>()))
                .Returns(Task.CompletedTask);

            try
            {
                _createResult = await _donationClient.CreateDonorOrganizationAsync(request);
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user registers a donor organization without a name")]
        public async Task WhenTheUserRegistersADonorOrganizationWithoutANameAsync()
        {
            try
            {
                _createResult = await _donationClient.CreateDonorOrganizationAsync(new CreateDonorOrganizationRequest
                {
                    Name = ""
                });
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user updates the donor organization with a new name")]
        public async Task WhenTheUserUpdatesTheDonorOrganizationWithANewNameAsync()
        {
            var id = _registeredOrganizationId ?? Constants.TestDonorOrganizationId;
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<DonorOrganization>()))
                .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.GetByNameAsync("Updated Corp"))
                .ReturnsAsync((DonorOrganization?)null);

            try
            {
                _updateResult = await _donationClient.UpdateDonorOrganizationAsync(id, new UpdateDonorOrganizationRequest
                {
                    Name = "Updated Corp"
                });
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user updates the donor organization with a name already in use")]
        public async Task WhenTheUserUpdatesTheDonorOrganizationWithANameAlreadyInUseAsync()
        {
            var id = _registeredOrganizationId ?? Constants.TestDonorOrganizationId;

            try
            {
                _updateResult = await _donationClient.UpdateDonorOrganizationAsync(id, new UpdateDonorOrganizationRequest
                {
                    Name = "Conflicting Corp"
                });
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [When("The user deletes the donor organization")]
        public async Task WhenTheUserDeletesTheDonorOrganizationAsync()
        {
            var id = _registeredOrganizationId ?? Constants.TestDonorOrganizationId;
            var mockRepo = _provider.GetRequiredService<Mock<IDonorOrganizationRepository>>();
            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            try
            {
                await _donationClient.DeleteDonorOrganizationAsync(id);
            }
            catch (ApiException<ProblemDetails> ex)
            {
                _context.LastException = ex;
            }
            catch (ApiException ex)
            {
                _context.LastException = ex;
            }
        }

        [Then("A paginated list of donor organizations is returned")]
        public void ThenAPaginatedListOfDonorOrganizationsIsReturned()
        {
            _context.LastException.Should().BeNull();
            _getOrganizationsResult.Should().NotBeNull();
            _getOrganizationsResult!.Organizations.Should().NotBeEmpty();
        }

        [Then("The donor organization details are returned")]
        public void ThenTheDonorOrganizationDetailsAreReturned()
        {
            _context.LastException.Should().BeNull();
            _getOrganizationByIdResult.Should().NotBeNull();
            _getOrganizationByIdResult!.Id.Should().NotBeNullOrEmpty();
            _getOrganizationByIdResult.Name.Should().NotBeNullOrEmpty();
        }

        [Then("The donor organization is registered on the time for code platform")]
        public void ThenTheDonorOrganizationIsRegisteredOnTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
            _createResult.Should().NotBeNull();
            _createResult!.Id.Should().NotBeNullOrEmpty();
            _createResult.Name.Should().Be("Acme Corp");
        }

        [Then("The donor organization is updated on the time for code platform")]
        public void ThenTheDonorOrganizationIsUpdatedOnTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
            _updateResult.Should().NotBeNull();
            _updateResult!.Name.Should().Be("Updated Corp");
        }

        [Then("The donor organization is deleted from the time for code platform")]
        public void ThenTheDonorOrganizationIsDeletedFromTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
        }
    }
}