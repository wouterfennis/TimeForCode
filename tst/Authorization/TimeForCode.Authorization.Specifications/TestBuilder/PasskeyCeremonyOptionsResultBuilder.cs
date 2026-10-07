using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    internal static class PasskeyCeremonyOptionsResultBuilder
    {
        public static PasskeyCeremonyOptionsResult Build()
        {
            return new PasskeyCeremonyOptionsResult
            {
                OptionsJson = "{}",
                ProtectedCeremonyState = "protected-ceremony-state"
            };
        }
    }
}