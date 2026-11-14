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
    public sealed record SessionResponse(bool IsSignedIn, bool IsNewUser, string UserId, string? Login, string? AvatarUrl);

    /// <summary>
    /// The browser-facing sign-in, session status and sign-out. The BFF holds the tokens; the browser holds one opaque cookie.
    /// </summary>
    internal static class BffEndpoints
    {
        private const string SignInFailedLocation = "/?signInError=1";

        public static void MapBffEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/bff");

            group.MapGet("/login", LoginAsync)
                .WithSummary("Starts the sign-in.")
                .WithDescription("Redirects the browser to the Authorization API, which redirects to the identity provider. The PKCE verifier stays on the server; a short-lived opaque cookie finds it again at the callback.")
                .Produces(StatusCodes.Status302Found);

            group.MapGet("/callback", CallbackAsync)
                .WithSummary("Completes the sign-in.")
                .WithDescription("The Authorization API redirects here with a single-use code. The BFF exchanges it, opens a session and redirects to the page the user came from. On any failure it redirects to /?signInError=1.")
                .Produces(StatusCodes.Status302Found);

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

            var signedIn = await sessions.CompleteLoginAsync(loginId, code, context.RequestAborted);
            if (signedIn is null)
            {
                return Results.Redirect(SignInFailedLocation);
            }

            BrowserSession.SetSessionCookie(context, signedIn.SessionId);
            return Results.Redirect(signedIn.ReturnUrl);
        }

        private static async Task<IResult> GetSessionAsync(HttpContext context, SessionService sessions)
        {
            context.Response.Headers.CacheControl = "no-store";
            var session = await BrowserSession.ResolveAsync(context, sessions);
            if (session is null)
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Not signed in");
            }

            var profile = SessionService.ReadProfile(session);
            return Results.Ok(new SessionResponse(true, sessions.IsNewUser(session), profile.UserId, profile.Login, profile.AvatarUrl));
        }

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