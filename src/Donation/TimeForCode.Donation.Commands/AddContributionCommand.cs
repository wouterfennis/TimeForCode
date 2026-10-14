using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class AddContributionCommand : IRequest<Result<AddContributionResult>>
    {
        public required string ProjectId { get; init; }
        public required string UserId { get; init; }
        public required string GithubLogin { get; init; }
        public required string AvatarUrl { get; init; }
        public required int Hours { get; init; }
        public Uri? GithubReference { get; init; }
    }
}