using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Interfaces
{
    public interface IGithubRepositoryApiService
    {
        Task<Result<GithubSnapshot>> GetRepositoryMetadataAsync(Uri repositoryUrl);
    }
}