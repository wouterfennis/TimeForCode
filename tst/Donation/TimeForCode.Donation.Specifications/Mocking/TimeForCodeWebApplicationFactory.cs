using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RestSharp;
using RichardSzalay.MockHttp;
using System.Text;
using TimeForCode.Donation.Api;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Infrastructure.Persistence.Database;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Donation.Specifications.Mocking
{
    public class TimeForCodeWebApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var interfacesToRemove = new List<Type>
                {
                    typeof(IMongoDbContext),
                    typeof(IProjectRepository),
                    typeof(IDonorOrganizationRepository),
                    typeof(IContributorRepository),
                    typeof(IMaintainerRepository),
                    typeof(IContributionRepository),
                    typeof(IDonationRepository),
                    typeof(IDonationAllocationRepository),
                    typeof(IGithubRepositoryApiService),
                    typeof(RestClient)
                };

                var descriptors = services
                    .Where(d => interfacesToRemove.Contains(d.ServiceType))
                    .ToList();

                if (descriptors.Count > 0)
                {
                    descriptors.ForEach(d => services.Remove(d));
                }

                MockDataAccess(services);

                // One host serves every scenario of a run, so the production limit of 60 requests a minute would reject later scenarios.
                services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
                services.Configure<RateLimiterOptions>(options => options.AddDefaultSlidingWindowPolicy("api", int.MaxValue));

                var mockHttp = new MockHttpMessageHandler();
                services.TryAddSingleton(mockHttp);

                var restClient = new RestClient(new RestClientOptions
                {
                    ConfigureMessageHandler = _ => mockHttp
                });
                services.TryAddSingleton(restClient);

                // Override JWT validation to accept symmetric test tokens
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.TestJwtSigningKey));
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey
                    };
                });
            });
        }

        /// <summary>Returns the shared host to its initial state, so one host can serve every scenario of a test run.</summary>
        public void ResetState()
        {
            Services.GetRequiredService<InMemoryContributorRepository>().Clear();
            Services.GetRequiredService<InMemoryMaintainerRepository>().Clear();
            Services.GetRequiredService<InMemoryContributionRepository>().Clear();
            Services.GetRequiredService<InMemoryDonationRepository>().Clear();
            Services.GetRequiredService<InMemoryDonationAllocationRepository>().Clear();

            Services.GetRequiredService<Mock<IProjectRepository>>().Reset();
            Services.GetRequiredService<Mock<IDonorOrganizationRepository>>().Reset();
            Services.GetRequiredService<Mock<IGithubRepositoryApiService>>().Reset();

            var mockHttp = Services.GetRequiredService<MockHttpMessageHandler>();
            mockHttp.ResetExpectations();
            mockHttp.ResetBackendDefinitions();
        }

        private static void MockDataAccess(IServiceCollection services)
        {
            var mockProjectRepository = new Mock<IProjectRepository>();
            services.TryAddSingleton(mockProjectRepository);
            services.TryAddScoped(_ => mockProjectRepository.Object);

            var mockDonorOrganizationRepository = new Mock<IDonorOrganizationRepository>();
            services.TryAddSingleton(mockDonorOrganizationRepository);
            services.TryAddScoped(_ => mockDonorOrganizationRepository.Object);

            var contributorRepository = new InMemoryContributorRepository();
            services.TryAddSingleton(contributorRepository);
            services.TryAddSingleton<IContributorRepository>(contributorRepository);

            var maintainerRepository = new InMemoryMaintainerRepository();
            services.TryAddSingleton(maintainerRepository);
            services.TryAddSingleton<IMaintainerRepository>(maintainerRepository);

            var contributionRepository = new InMemoryContributionRepository();
            services.TryAddSingleton(contributionRepository);
            services.TryAddSingleton<IContributionRepository>(contributionRepository);

            var donationRepository = new InMemoryDonationRepository();
            services.TryAddSingleton(donationRepository);
            services.TryAddSingleton<IDonationRepository>(donationRepository);

            var allocationRepository = new InMemoryDonationAllocationRepository();
            services.TryAddSingleton(allocationRepository);
            services.TryAddSingleton<IDonationAllocationRepository>(allocationRepository);

            var mockGithubService = new Mock<IGithubRepositoryApiService>();
            services.TryAddSingleton(mockGithubService);
            services.TryAddScoped(_ => mockGithubService.Object);
        }
    }
}