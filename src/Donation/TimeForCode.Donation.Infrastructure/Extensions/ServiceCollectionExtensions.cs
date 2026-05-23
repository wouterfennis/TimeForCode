using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Infrastructure.Options;
using TimeForCode.Donation.Infrastructure.Persistence.Database;
using TimeForCode.Donation.Infrastructure.Services;

namespace TimeForCode.Donation.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .Configure<DbOptions>(options => configuration.GetSection(DbOptions.SectionName)
                .Bind(options));

            services.AddSingleton<RestClient>();
            services.AddSingleton<IMongoDbContext, MongoDbContext>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IGithubRepositoryApiService, GithubRepositoryApiService>();

            return services;
        }
    }
}