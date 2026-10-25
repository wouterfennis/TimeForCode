using AwesomeAssertions;
using Reqnroll;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace TimeForCode.Authorization.Specifications.Steps
{
    internal partial class LoginSteps
    {
        [Then("The redirect to the external platform contains a code challenge")]
        public void ThenTheRedirectToTheExternalPlatformContainsACodeChallenge()
        {
            var query = HttpUtility.ParseQueryString(new Uri(_result!.Exception!.Headers["Location"].Single()).Query);

            query["code_challenge"].Should().NotBeNullOrEmpty();
            query["code_challenge_method"].Should().Be("S256");
        }

        [Then("The time for code platform proved to the external platform that it started the login")]
        public void ThenTheTimeForCodePlatformProvedToTheExternalPlatformThatItStartedTheLogin()
        {
            var challenge = HttpUtility.ParseQueryString(new Uri(_result!.Exception!.Headers["Location"].Single()).Query)["code_challenge"];

            var tokenRequest = JsonDocument.Parse(_infrastructure.AccessTokenRequests.Single()).RootElement;
            var verifier = tokenRequest.GetProperty("code_verifier").GetString()!;

            Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_')
                .Should().Be(challenge);
        }
    }
}