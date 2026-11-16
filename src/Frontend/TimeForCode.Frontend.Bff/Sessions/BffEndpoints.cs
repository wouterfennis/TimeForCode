using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>Where the browser reaches the Authorization API (configuration section <c>Authorization</c>).</summary>
    public sealed class AuthorizationOptions
    {
        public const string SectionName = "Authorization";

        /// <summary>
        /// The address the BFF itself calls for the code exchange, refresh and logout (internal network).
        /// </summary>
        public string BaseAddress { get; init; } = string.Empty;

        /// <summary>
        /// The public address the browser is sent to for the login; the Authorization API answers with a redirect to the identity provider.
        /// </summary>
        public string PublicBaseAddress { get; init; } = string.Empty;
    }

    /// <summary>What the SPA learns about the session: who the user is and whether it is their first sign-in. Never a token.</summary>
    /// <param name="IsSignedIn">Always true: a missing session answers 401 instead.</param>
    /// <param name="IsNewUser">True for a short time after the first ever sign-in, so the welcome shows once.</param>
    /// <param name="UserId">The platform id of the user.</param>
    /// <param name="Login">The login name at the identity provider.</param>
    /// <param name="AvatarUrl">The avatar of the user at the identity provider.</param>
    /// <param name="IsAdmin">True for the administrator, who signed in with a passkey.</param>
    public sealed record SessionResponse(bool IsSignedIn, bool IsNewUser, bool IsAdmin, string UserId, string? Login, string? AvatarUrl);

    /// <summary>The options for the browser passkey call and the id that completes the ceremony.</summary>
    /// <param name="CeremonyId">Send it back when the ceremony completes. It works once and expires after a short time.</param>
    /// <param name="OptionsJson">The WebAuthn options as JSON: pass them to PublicKeyCredential.parseCreationOptionsFromJSON or parseRequestOptionsFromJSON.</param>
    public sealed record AdminCeremonyResponse(string CeremonyId, string OptionsJson);

    /// <summary>Starts the admin passkey registration.</summary>
    /// <param name="BootstrapSecret">The secret that proves the caller may claim the admin role.</param>
    public sealed record AdminRegistrationOptionsRequest(string BootstrapSecret);

    /// <summary>Completes the admin passkey registration.</summary>
    /// <param name="BootstrapSecret">The same bootstrap secret.</param>
    /// <param name="CredentialJson">The credential the browser created, as JSON (PublicKeyCredential.toJSON()).</param>
    /// <param name="CeremonyId">The ceremony id of the options call.</param>
    public sealed record AdminRegistrationRequest(string BootstrapSecret, string CredentialJson, string CeremonyId);

    /// <summary>Completes the admin passkey sign-in.</summary>
    /// <param name="CredentialJson">The assertion the browser produced, as JSON (PublicKeyCredential.toJSON()).</param>
    /// <param name="CeremonyId">The ceremony id of the options call.</param>
    public sealed record AdminAuthenticationRequest(string CredentialJson, string CeremonyId);

    /// <summary>
    /// The browser-facing sign-in, session status and sign-out. The BFF holds the tokens; the browser holds one opaque cookie.
    /// </summary>
    internal static class BffEndpoints
    {
        private const string SignInFailedLocation = "/?signInError=1";
        private const string NoStore = "no-store";

        public static void MapBffEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/bff");

            group.MapGet("/login", LoginAsync)
                .WithSummary("Starts the sign-in.")
                .WithDescription("Redirects the browser to the Authorization API, which redirects to the identity provider. The PKCE verifier stays on the server; a short-lived opaque cookie finds it again at the callback.")
                .Produces(StatusCodes.Status302Found)
                .ProducesProblem(StatusCodes.Status429TooManyRequests)
                .RequireRateLimiting(SignInRateLimit.PolicyName);

            group.MapGet("/callback", CallbackAsync)
                .WithSummary("Completes the sign-in.")
                .WithDescription("The Authorization API redirects here with a single-use code. The BFF exchanges it, opens a session and redirects to the page the user came from. On any failure it redirects to /?signInError=1.")
                .Produces(StatusCodes.Status302Found)
                .ProducesProblem(StatusCodes.Status429TooManyRequests)
                .RequireRateLimiting(SignInRateLimit.PolicyName);

            group.MapGet("/session", GetSessionAsync)
                .WithSummary("Tells whether the user is signed in.")
                .WithDescription("Call it when the SPA loads. It returns who the user is and whether this is their first sign-in; it never returns a token.")
                .Produces<SessionResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            group.MapPost("/logout", LogoutAsync)
                .WithSummary("Signs out.")
                .WithDescription("Revokes the refresh token, ends the session and clears the session cookie. Needs the X-Requested-With header; a request from another origin is refused with 403.")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status403Forbidden);

            MapAdminEndpoints(group);

            // An unknown /bff path is not the SPA.
            group.MapFallback(() => Results.NotFound());
        }

        private static async Task<IResult> LoginAsync(HttpContext context, SessionService sessions, IOptions<AuthorizationOptions> authorization, IOptions<BrowserSessionOptions> sessionOptions,
            [System.ComponentModel.Description("The page to return to after sign-in: a path on this site such as /projects/42. Anything else is replaced by /.")] string? returnUrl)
        {
            var start = await sessions.StartLoginAsync(returnUrl, context.RequestAborted);
            BrowserSession.SetLoginCookie(context, start.LoginId, TimeSpan.FromMinutes(sessionOptions.Value.LoginTimeoutMinutes));

            var callback = $"{context.Request.Scheme}://{context.Request.Host}/bff/callback";
            var login = new UriBuilder(new Uri(new Uri(authorization.Value.PublicBaseAddress), "api/v1/authentication/login"))
            {
                Query = $"IdentityProvider=Github&RedirectUri={Uri.EscapeDataString(callback)}&CodeChallenge={start.CodeChallenge}"
            };
            return Results.Redirect(login.Uri.AbsoluteUri);
        }

        private static async Task<IResult> CallbackAsync(
            HttpContext context,
            SessionService sessions,
            [System.ComponentModel.Description("The single-use code the Authorization API appends to the redirect. It is exchanged once with the PKCE verifier of this browser.")] string? code)
        {
            context.Request.Cookies.TryGetValue(BrowserSession.LoginCookie, out var loginId);
            BrowserSession.ClearLoginCookie(context);

            context.Request.Cookies.TryGetValue(BrowserSession.SessionCookie, out var previousSessionId);
            var signedIn = await sessions.CompleteLoginAsync(loginId, code, previousSessionId, context.RequestAborted);
            if (signedIn is null)
            {
                return Results.Redirect(SignInFailedLocation);
            }

            BrowserSession.SetSessionCookie(context, signedIn.SessionId);
            return Results.Redirect(signedIn.ReturnUrl);
        }

        private static async Task<IResult> GetSessionAsync(HttpContext context, SessionService sessions)
        {
            context.Response.Headers.CacheControl = NoStore;
            var session = await BrowserSession.ResolveAsync(context, sessions);
            if (session is null)
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Not signed in");
            }

            var profile = SessionService.TryReadProfile(session);
            if (profile is null)
            {
                // A token the BFF cannot read is no usable session.
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Not signed in");
            }

            return Results.Ok(new SessionResponse(true, sessions.IsNewUser(session), profile.Scope == "admin", profile.UserId, profile.Login, profile.AvatarUrl));
        }

        private static void MapAdminEndpoints(RouteGroupBuilder group)
        {
            var admin = group.MapGroup("/admin");
            admin.RequireRateLimiting(SignInRateLimit.PolicyName);
            admin.WithMetadata(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json"));

            admin.MapPost("/registration/options", AdminRegistrationOptionsAsync)
                .WithSummary("Starts the admin passkey registration.")
                .WithDescription("Only while no admin passkey is registered. Returns the options for navigator.credentials.create and a ceremony id; the ceremony state stays on the server and works once.")
                .Produces<AdminCeremonyResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            admin.MapPost("/registration", AdminRegistrationAsync)
                .WithSummary("Completes the admin passkey registration and signs the admin in.")
                .WithDescription("Claims the admin role for the passkey, opens the session and sets the session cookie. The response never holds a token.")
                .Produces<SessionResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            admin.MapPost("/authentication/options", AdminAuthenticationOptionsAsync)
                .WithSummary("Starts the admin passkey sign-in.")
                .WithDescription("Returns the options for navigator.credentials.get and a ceremony id that works once.")
                .Produces<AdminCeremonyResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            admin.MapPost("/authentication", AdminAuthenticationAsync)
                .WithSummary("Completes the admin passkey sign-in.")
                .WithDescription("Opens the admin session and sets the session cookie. The response never holds a token.")
                .Produces<SessionResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> AdminRegistrationOptionsAsync(HttpContext context, IAuthorizationApi api, AdminRegistrationOptionsRequest request)
        {
            context.Response.Headers.CacheControl = NoStore;
            var ceremony = await api.AdminRegistrationOptionsAsync(request.BootstrapSecret, context.RequestAborted);
            return ceremony is null ? AdminRejected() : Results.Ok(new AdminCeremonyResponse(ceremony.CeremonyId, ceremony.OptionsJson));
        }

        private static async Task<IResult> AdminRegistrationAsync(HttpContext context, IAuthorizationApi api, SessionService sessions, AdminRegistrationRequest request)
        {
            context.Response.Headers.CacheControl = NoStore;
            var tokens = await api.AdminCompleteRegistrationAsync(request.BootstrapSecret, request.CredentialJson, request.CeremonyId, context.RequestAborted);
            return await OpenAdminSessionAsync(context, sessions, tokens);
        }

        private static async Task<IResult> AdminAuthenticationOptionsAsync(HttpContext context, IAuthorizationApi api)
        {
            context.Response.Headers.CacheControl = NoStore;
            var ceremony = await api.AdminAuthenticationOptionsAsync(context.RequestAborted);
            return ceremony is null ? AdminRejected() : Results.Ok(new AdminCeremonyResponse(ceremony.CeremonyId, ceremony.OptionsJson));
        }

        private static async Task<IResult> AdminAuthenticationAsync(HttpContext context, IAuthorizationApi api, SessionService sessions, AdminAuthenticationRequest request)
        {
            context.Response.Headers.CacheControl = NoStore;
            var tokens = await api.AdminCompleteAuthenticationAsync(request.CredentialJson, request.CeremonyId, context.RequestAborted);
            return await OpenAdminSessionAsync(context, sessions, tokens);
        }

        // The tokens stay here: only the opaque cookie and who the admin is go to the browser.
        private static async Task<IResult> OpenAdminSessionAsync(HttpContext context, SessionService sessions, IssuedTokens? tokens)
        {
            // A token the BFF cannot read is no usable session, so nothing is opened for it.
            var profile = tokens is null ? null : SessionService.TryReadAccessTokenProfile(tokens.AccessToken);
            if (tokens is null || profile is null)
            {
                return AdminRejected();
            }

            context.Request.Cookies.TryGetValue(BrowserSession.SessionCookie, out var previousSessionId);
            var sessionId = await sessions.OpenSessionAsync(tokens, previousSessionId);
            BrowserSession.SetSessionCookie(context, sessionId);

            return Results.Ok(new SessionResponse(true, false, profile.Scope == "admin", profile.UserId, profile.Login, profile.AvatarUrl));
        }

        // One generic answer for every refusal, so the browser learns nothing about the reason (wrong secret, claimed role, wrong device, used ceremony).
        private static IResult AdminRejected() =>
            Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The admin sign-in could not be completed");

        private static async Task<IResult> LogoutAsync(HttpContext context, SessionService sessions)
        {
            var session = await BrowserSession.ResolveAsync(context, sessions);
            if (session is not null)
            {
                await sessions.SignOutAsync(session, context.RequestAborted);
                BrowserSession.ClearSessionCookie(context);
            }

            // A cookie that maps to no session was already queued for clearing by ResolveAsync.
            return Results.NoContent();
        }
    }
}