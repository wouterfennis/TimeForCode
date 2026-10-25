using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using TimeForCode.Authorization.Api.Controllers;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Tests.Controllers
{
    [TestClass]
    public class JwksControllerTests
    {
        private static List<JwkResponse> GetKeys(SigningKeySet signingKeys)
        {
            var sut = new JwksController(NullLogger<JwksController>.Instance, signingKeys);

            var result = sut.GetJwksJson();

            var jwks = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<JwksResponse>().Subject;
            return jwks.Keys.ToList();
        }

        [TestMethod]
        public void GetJwksJson_SingleKey_DescribesKeyIdUseAndAlgorithm()
        {
            var signingKeys = new SigningKeySet(RSA.Create(2048));

            var keys = GetKeys(signingKeys);

            var key = keys.Should().ContainSingle().Subject;
            key.KeyId.Should().Be(signingKeys.Current.KeyId);
            key.Use.Should().Be("sig");
            key.Algorithm.Should().Be("RS256");
        }

        [TestMethod]
        public void GetJwksJson_CurrentAndPreviousKey_PublishesBoth()
        {
            var signingKeys = new SigningKeySet(RSA.Create(2048), RSA.Create(2048));

            var keys = GetKeys(signingKeys);

            keys.Select(k => k.KeyId).Should().BeEquivalentTo([signingKeys.Current.KeyId, signingKeys.Previous!.KeyId]);
        }
    }
}