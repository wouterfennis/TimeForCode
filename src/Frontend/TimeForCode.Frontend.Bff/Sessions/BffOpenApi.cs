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
            [("bff/logout", "403")] = "The X-Requested-With header is missing, or the Origin header names another site.",
            [("bff/admin/registration/options", "200")] = "The options for navigator.credentials.create and the ceremony id that completes the registration. No cookie is set.",
            [("bff/admin/registration/options", "400")] = "The bootstrap secret is wrong or an admin passkey is already registered. The reason is not given.",
            [("bff/admin/registration", "200")] = "The admin passkey is registered and the admin is signed in: the session cookie is set and the body says who the user is; it never holds a token.",
            [("bff/admin/registration", "400")] = "The registration was refused (wrong secret, ceremony unknown, expired or already used, passkey rejected). No session is opened.",
            [("bff/admin/authentication/options", "200")] = "The options for navigator.credentials.get and the ceremony id that completes the sign-in. No cookie is set.",
            [("bff/admin/authentication/options", "400")] = "No admin passkey is registered yet.",
            [("bff/admin/authentication", "200")] = "The admin is signed in: the session cookie is set and the body says who the user is; it never holds a token.",
            [("bff/admin/authentication", "400")] = "The sign-in was refused (ceremony unknown, expired or already used, wrong device, signature counter did not increase). No session is opened.",
            [("bff/admin/registration/options", "403")] = "The X-Requested-With header is missing, or the Origin header names another site.",
            [("bff/admin/registration", "403")] = "The X-Requested-With header is missing, or the Origin header names another site.",
            [("bff/admin/authentication/options", "403")] = "The X-Requested-With header is missing, or the Origin header names another site.",
            [("bff/admin/authentication", "403")] = "The X-Requested-With header is missing, or the Origin header names another site."
        };

        private static readonly Dictionary<string, string> BodyDescriptions = new()
        {
            ["bff/admin/registration/options"] = "The bootstrap secret that proves the caller may claim the admin role.",
            ["bff/admin/registration"] = "The bootstrap secret, the credential the browser created (PublicKeyCredential.toJSON()) and the ceremony id of the options call.",
            ["bff/admin/authentication"] = "The assertion the browser produced (PublicKeyCredential.toJSON()) and the ceremony id of the options call."
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

                if (operation.RequestBody is not null && string.IsNullOrEmpty(operation.RequestBody.Description))
                {
                    operation.RequestBody.Description = BodyDescriptions.GetValueOrDefault(path, "The JSON request body.");
                }

                return Task.CompletedTask;
            });
    }
}