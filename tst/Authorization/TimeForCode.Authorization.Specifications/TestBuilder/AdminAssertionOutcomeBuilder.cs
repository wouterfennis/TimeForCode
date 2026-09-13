using TimeForCode.Authorization.Application.Interfaces.Admin;

namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    internal static class AdminAssertionOutcomeBuilder
    {
        public static AdminAssertionOutcome BuildSucceeded()
        {
            return new AdminAssertionOutcome { Succeeded = true, SignCount = 1 };
        }

        public static AdminAssertionOutcome BuildFailed()
        {
            return new AdminAssertionOutcome { Succeeded = false, ErrorMessage = "Assertion failed." };
        }
    }
}