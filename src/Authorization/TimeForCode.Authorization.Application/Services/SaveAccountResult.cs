using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Services
{
    public record SaveAccountResult(AccountInformation AccountInformation, bool IsNewAccount);
}