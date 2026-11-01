using TimeForCode.Shared.Live;

namespace TimeForCode.Seeder
{
    /// <summary>Addresses and secret of the stack to seed. Arguments win over environment variables, which win over the local compose defaults.</summary>
    internal sealed record SeederOptions(Uri AuthorizationUrl, Uri DonationUrl, Uri ClientRedirectUri, string AdminSecret)
    {
        public const string DonationUrlVariable = "TIMEFORCODE_LIVE_DONATION_URL";

        private const string AuthorizationUrlOption = "--authorization-url";
        private const string DonationUrlOption = "--donation-url";
        private const string ClientRedirectUriOption = "--redirect-uri";
        private const string AdminSecretOption = "--admin-secret";

        private const string DefaultAuthorizationUrl = "http://localhost:8080"; // NOSONAR S1075: local compose address, overridable
        private const string DefaultDonationUrl = "http://localhost:8082"; // NOSONAR S1075: local compose address, overridable
        private const string DefaultClientRedirectUri = "http://localhost:8084"; // NOSONAR S1075: local compose address, overridable

        public static SeederOptions Resolve(IReadOnlyList<string> args, Func<string, string?> environment)
        {
            Dictionary<string, string> given = ParseArguments(args);

            string? secret = Pick(given, AdminSecretOption, environment, LiveAuthentication.TestAdminSecretVariable);
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new SeederException(
                    $"The admin secret is missing: pass {AdminSecretOption} or set {LiveAuthentication.TestAdminSecretVariable} to the TEST_ADMIN_ACCESS_SECRET the stack was started with.");
            }

            return new SeederOptions(
                ToUri(AuthorizationUrlOption, Pick(given, AuthorizationUrlOption, environment, LiveAuthentication.AuthorizationUrlVariable) ?? DefaultAuthorizationUrl),
                ToUri(DonationUrlOption, Pick(given, DonationUrlOption, environment, DonationUrlVariable) ?? DefaultDonationUrl),
                ToUri(ClientRedirectUriOption, Pick(given, ClientRedirectUriOption, environment, LiveAuthentication.ClientRedirectUriVariable) ?? DefaultClientRedirectUri),
                secret);
        }

        private static Dictionary<string, string> ParseArguments(IReadOnlyList<string> args)
        {
            string[] known = [AuthorizationUrlOption, DonationUrlOption, ClientRedirectUriOption, AdminSecretOption];
            var given = new Dictionary<string, string>();

            for (int i = 0; i < args.Count; i += 2)
            {
                if (!known.Contains(args[i]))
                {
                    throw new SeederException($"Unknown option '{args[i]}'. Known options: {string.Join(", ", known)}.");
                }

                if (i + 1 >= args.Count)
                {
                    throw new SeederException($"Option {args[i]} needs a value.");
                }

                given[args[i]] = args[i + 1];
            }

            return given;
        }

        private static string? Pick(Dictionary<string, string> given, string option, Func<string, string?> environment, string variable)
        {
            if (given.TryGetValue(option, out string? value))
            {
                return value;
            }

            string? fromEnvironment = environment(variable);
            return string.IsNullOrWhiteSpace(fromEnvironment) ? null : fromEnvironment;
        }

        private static Uri ToUri(string option, string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new SeederException($"{option} must be an absolute http(s) URL, but was '{value}'.");
            }

            return uri;
        }
    }
}