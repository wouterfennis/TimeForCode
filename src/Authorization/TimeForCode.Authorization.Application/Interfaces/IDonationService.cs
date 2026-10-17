using MediatR;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IDonationService
    {
        /// <summary>
        /// Asks the donation module to replace the identity of the contributor of a deleted account with a generic deleted user.
        /// </summary>
        Task<Result<Unit>> AnonymiseContributorAsync(string userId);
    }
}