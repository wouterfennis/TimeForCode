using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RestSharp;
using RichardSzalay.MockHttp;
using System.Text;
using TimeForCode.Donation.Api;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Infrastructure.Persistence.Database;

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

        private static void MockDataAccess(IServiceCollection services)
        {
            var mockProjectRepository = new Mock<IProjectRepository>();
            services.TryAddSingleton(mockProjectRepository);
            services.TryAddScoped(_ => mockProjectRepository.Object);

            var mockGithubService = new Mock<IGithubRepositoryApiService>();
            services.TryAddSingleton(mockGithubService);
            services.TryAddScoped(_ => mockGithubService.Object);
        }
    }
}