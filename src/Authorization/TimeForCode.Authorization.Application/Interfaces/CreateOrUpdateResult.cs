using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public record CreateOrUpdateResult(AccountInformation AccountInformation, bool IsNewAccount);
}