namespace TimeForCode.Authorization.Api
{
    /// <summary>
    /// Input limits shared by the Authorization API; declared as validation attributes so they appear in the OpenAPI document.
    /// </summary>
    internal static class ApiConstraints
    {
        internal const int MaxOAuthParameterLength = 2048;
        internal const int MaxSecretLength = 256;
        internal const int MaxCredentialJsonLength = 16384;
        internal const int MaxGithubOwnerLength = 39;
        internal const int MaxGithubRepositoryNameLength = 100;

        // The trailing '$' is safe despite .NET letting it match before a final '\n': RegularExpressionAttribute
        // only accepts a value when the match spans the whole string, so "octocat\n" is rejected (see Repositories.feature).

        // GitHub user and organisation names: alphanumeric, with single hyphens that are neither first nor last.
        internal const string GithubOwnerPattern = "^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$";

        // GitHub repository names: letters, digits, '.', '_' and '-', but never just '.' or '..'.
        internal const string GithubRepositoryNamePattern = @"^(?!\.{1,2}$)[A-Za-z0-9._-]+$";
    }
}