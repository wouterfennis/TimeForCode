using MediatR;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Commands
{
    /// <summary>
    /// Returns a contributor profile: the caller's own when <see cref="ContributorId"/> is empty, otherwise the profile with that id (own profile or administrator only).
    /// </summary>
    public class GetContributorProfileQuery : IRequest<Result<ContributorProfileResult>>
    {
        public required Actor Actor { get; init; }
        public string? ContributorId { get; init; }
    }
}