using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class RegisterProjectCommand : IRequest<Result<RegisterProjectResult>>
    {
        public required Uri GithubRepositoryUrl { get; init; }
        public required string UserId { get; init; }
        public required string GithubLogin { get; init; }
        public required string AvatarUrl { get; init; }
    }
}