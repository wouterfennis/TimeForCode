using IdentityProviderMockService.Models;
using IdentityProviderMockService.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Web;

namespace IdentityProviderMockService.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [Route("login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly RsaSecurityKey _rsaSecurityKey;
        private readonly AuthenticationOptions _authenticationOptions;

        public LoginController(ILogger<LoginController> logger,
            IOptions<AuthenticationOptions> authenticationOptions,
            RSA rsa,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _authenticationOptions = authenticationOptions.Value;

            _rsaSecurityKey = new RsaSecurityKey(rsa);
        }

        [HttpGet("oauth/authorize")]
        [Produces("text/html")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult GetAuthorize(AuthorizeRequest request)
        {
            _logger.LogDebug("authorize page is shown");

            return Content(BuildLoginHtml(request), "text/html");
        }

        [HttpPost("oauth/confirm")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult PostConfirm([FromForm] ConfirmRequest request)
        {
            _logger.LogDebug("authorize is confirmed");

            string code = Guid.NewGuid().ToString("N");
            _memoryCache.Set(code, new AuthorizeRequest
            {
                State = request.State,
                RedirectUri = request.RedirectUri,
                Scope = request.Scope,
                ClientId = request.ClientId
            });

            var uriBuilder = new UriBuilder(request.RedirectUri);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["state"] = request.State;
            query["code"] = code;

            uriBuilder.Query = query.ToString();

            return Redirect(uriBuilder.ToString());
        }

        [HttpPost("oauth/access_token")]
        [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAccessToken([FromBody] AccessTokenRequest accessTokenRequest)
        {
            if (accessTokenRequest.ClientId != _authenticationOptions.ExpectedClientId || accessTokenRequest.ClientSecret != _authenticationOptions.ExpectedClientSecret)
            {
                _logger.LogDebug("Token is not returned");
                var problemDetails = new ProblemDetails
                {
                    Detail = "ClientId and secret not known",
                    Status = 404
                };

                return NotFound(problemDetails);
            }

            var codeExists = _memoryCache.TryGetValue(accessTokenRequest.Code, out AuthorizeRequest? authorizeDetails);
            if (!codeExists)
            {
                _logger.LogDebug("Authorization code is not found");
                var problemDetails = new ProblemDetails
                {
                    Detail = "Authorization code is not found",
                    Status = 404
                };

                return NotFound(problemDetails);
            }

            _logger.LogDebug("Token is returned");
            var claims = new Dictionary<string, object>
            {
                { "scope", authorizeDetails!.Scope }
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("sub", authorizeDetails!.ClientId)]),
                Expires = DateTime.UtcNow.AddMinutes(_authenticationOptions.TokenExpiresInMinutes),
                Issuer = _authenticationOptions.Issuer,
                Audience = _authenticationOptions.Audience,
                Claims = claims,
                SigningCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new AccessTokenResponse
            {
                TokenType = "Bearer",
                AccessToken = tokenString,
                Scope = authorizeDetails.Scope
            });
        }

        private static string BuildLoginHtml(AuthorizeRequest request) => $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Sign in &ndash; Mock Identity Provider</title>
              <style>
                *, *::before, *::after { box-sizing: border-box; }
                body {
                  margin: 0; min-height: 100vh; display: flex; align-items: center;
                  justify-content: center; background: #f6f8fa;
                  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
                  color: #24292f;
                }
                .card {
                  background: #fff; border: 1px solid #d0d7de; border-radius: 6px;
                  padding: 2rem 2.5rem; width: 100%; max-width: 360px;
                  box-shadow: 0 1px 3px rgba(31,35,40,.12);
                }
                .logo { text-align: center; margin-bottom: 1.25rem; font-size: 1.5rem; }
                h1 { font-size: 1.25rem; font-weight: 600; text-align: center; margin: 0 0 1.5rem; }
                .user-badge {
                  display: flex; align-items: center; gap: .75rem; padding: .75rem 1rem;
                  background: #f6f8fa; border: 1px solid #d0d7de; border-radius: 6px;
                  margin-bottom: 1.5rem;
                }
                .avatar {
                  width: 36px; height: 36px; border-radius: 50%;
                  background: #0969da; color: #fff;
                  display: flex; align-items: center; justify-content: center;
                  font-weight: 700; font-size: .95rem; flex-shrink: 0;
                }
                .user-info small { color: #57606a; display: block; font-size: .75rem; }
                .scope-note { font-size: .8125rem; color: #57606a; margin-bottom: 1.5rem; }
                button {
                  width: 100%; padding: .6rem 1rem; background: #2da44e; color: #fff;
                  border: none; border-radius: 6px; font-size: .9375rem; font-weight: 600;
                  cursor: pointer; transition: background .15s;
                }
                button:hover { background: #2c974b; }
                .mock-notice { text-align: center; margin-top: 1.25rem; font-size: .75rem; color: #57606a; }
              </style>
            </head>
            <body>
              <div class="card">
                <div class="logo">&#128274;</div>
                <h1>Authorize application</h1>
                <div class="user-badge">
                  <div class="avatar">M</div>
                  <div class="user-info">
                    <strong>mock-user</strong>
                    <small>Signed in as the mock user</small>
                  </div>
                </div>
                <p class="scope-note">
                  <strong>{{HtmlEncode(request.ClientId)}}</strong> is requesting access to your account
                  with scope <code>{{HtmlEncode(request.Scope)}}</code>.
                </p>
                <form method="post" action="/login/oauth/confirm">
                  <input type="hidden" name="state"        value="{{HtmlEncode(request.State)}}" />
                  <input type="hidden" name="redirect_uri" value="{{HtmlEncode(request.RedirectUri)}}" />
                  <input type="hidden" name="client_id"    value="{{HtmlEncode(request.ClientId)}}" />
                  <input type="hidden" name="scope"        value="{{HtmlEncode(request.Scope)}}" />
                  <button type="submit">Authorize</button>
                </form>
                <p class="mock-notice">&#9432;&nbsp;This is a <strong>mock</strong> identity provider for testing.</p>
              </div>
            </body>
            </html>
            """;

        private static string HtmlEncode(string value) =>
            System.Net.WebUtility.HtmlEncode(value);
    }
}