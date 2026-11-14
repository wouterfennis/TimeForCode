using Microsoft.AspNetCore.OpenApi;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// The response descriptions of the BFF endpoints in the OpenAPI document. Minimal APIs only produce the generic status
    /// text ("Found", "Unauthorized"), which tells the SPA developer nothing about what happened or which cookie changed.
    /// </summary>
    internal static class BffOpenApi
    {
        private static readonly Dictionary<(string Path, string Status), string> Descriptions = new()
        {
            [("bff/login", "302")] = "Redirect to the Authorization API login, which redirects on to the identity provider. Sets the short-lived __Host-login cookie that finds the PKCE verifier again.",
            [("bff/callback", "302")] = "Redirect to the page the user came from after a successful sign-in, with the __Host-session cookie set; or to /?signInError=1 when the sign-in could not be completed.",
            [("bff/session", "200")] = "The user is signed in. The body says who the user is and whether this is their first sign-in; it never holds a token.",
            [("bff/session", "401")] = "Nobody is signed in: there is no session cookie, or the session expired or was ended.",
            [("bff/logout", "204")] = "The session is ended, the refresh token revoked and the session cookie cleared. Also returned when there was no session.",
            [("bff/logout", "403")] = "The X-Requested-With header is missing, or the Origin header names another site."
        };

        public static void Describe(OpenApiOptions options) =>
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var path = context.Description.RelativePath ?? string.Empty;
                foreach (var (status, response) in operation.Responses ?? [])
                {
                    if (Descriptions.TryGetValue((path, status), out var description))
                    {
                        response.Description = description;
                    }
                }

                return Task.CompletedTask;
            });
    }
}