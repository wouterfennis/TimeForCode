using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal static class ContributionChangeMapper
    {
        /// <summary>A deleted contribution is reported as not found, like one that never existed.</summary>
        internal static Result<ContributionChangedResult> ToFailure(this ContributionChange change)
        {
            return change.Failure == ContributionChangeFailure.NotAllowed
                ? Result<ContributionChangedResult>.Forbidden("Only the contributor or an administrator can change a contribution.")
                : Result<ContributionChangedResult>.NotFound("Contribution not found.");
        }
    }
}