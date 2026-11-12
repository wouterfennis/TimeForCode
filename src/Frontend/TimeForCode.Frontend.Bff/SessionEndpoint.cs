using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Frontend.Bff
{
    /// <summary>
    /// <c>GET /api/session</c> tells the SPA who is signed in, from the claims of the HttpOnly access-token cookie,
    /// which the SPA cannot read. The token is validated like the APIs do (signature from the Authorization API's
    /// published keys, issuer, audience, lifetime); anything else counts as signed out. The token itself is never returned.
    /// The answer only steers the UI: the APIs still enforce authorization.
    /// </summary>
    internal static class SessionEndpoint
    {
        public const string Path = "/api/session";

        public static IServiceCollection AddSessionEndpoint(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddJwtBearerAuthentication(configuration);
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents { OnMessageReceived = ReadTokenFromCookie };
            });
            return services;
        }

        public static void MapSessionEndpoint(this WebApplication app) => app.MapGet(Path, async (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            AuthenticateResult result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Results.Json(new Session(false, null, null, false));
            }

            ClaimsPrincipal user = result.Principal;
            return Results.Json(new Session(true, user.FindFirstValue("login"), user.FindFirstValue("avatar_url"), user.HasClaim("role", "admin")));
        });

        // Only the cookie counts: an Authorization header sent by the browser is ignored.
        private static Task ReadTokenFromCookie(MessageReceivedContext context)
        {
            string? token = TryReadToken(context.Request.Cookies[CookieConstants.TokenKey]);
            if (token is null)
            {
                context.NoResult();
            }
            else
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        }

        private static string? TryReadToken(string? cookie)
        {
            if (string.IsNullOrEmpty(cookie))
            {
                return null;
            }

            try
            {
                string? token = JsonSerializer.Deserialize<AccessToken>(cookie)?.Token;
                return string.IsNullOrEmpty(token) ? null : token;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private sealed record Session(bool SignedIn, string? Login, string? AvatarUrl, bool IsAdmin);
    }
}