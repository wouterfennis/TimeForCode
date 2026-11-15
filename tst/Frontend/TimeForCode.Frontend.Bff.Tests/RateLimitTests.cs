using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class RateLimitTests
    {
        private const int Limit = 3;

        public TestContext TestContext { get; set; } = null!;

        private BffFactory _factory = null!;
        private HttpClient _browser = null!;

        [TestInitialize]
        public void Initialize()
        {
            _factory = new BffFactory();
            _browser = _factory.WithWebHostBuilder(builder => builder.UseSetting("Session:SignInRequestsPerMinute", Limit.ToString()))
                .CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        }

        [TestCleanup]
        public void Cleanup()
        {
            _browser.Dispose();
            _factory.Dispose();
        }

        private async Task<HttpStatusCode[]> SendManyAsync(Func<HttpRequestMessage> request, int count)
        {
            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < count; i++)
            {
                using var response = await _browser.SendAsync(request(), TestContext.CancellationToken);
                statuses.Add(response.StatusCode);
            }

            return [.. statuses];
        }

        [TestMethod]
        [DataRow("/bff/login")]
        [DataRow("/bff/callback?code=x")]
        public async Task SignInEndpoint_MoreRequestsThanTheLimitFromOneClient_AreRefusedWith429(string path)
        {
            var statuses = await SendManyAsync(() => new HttpRequestMessage(HttpMethod.Get, path), Limit + 1);

            statuses.Take(Limit).Should().NotContain(HttpStatusCode.TooManyRequests);
            statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
        }

        [TestMethod]
        public async Task SignInEndpoint_RefusedRequest_TellsWhenToRetry()
        {
            await SendManyAsync(() => new HttpRequestMessage(HttpMethod.Get, "/bff/login"), Limit);

            using var refused = await _browser.GetAsync("/bff/login", TestContext.CancellationToken);

            refused.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            refused.Headers.RetryAfter.Should().NotBeNull();
        }

        [TestMethod]
        [DataRow("/bff/admin/registration/options")]
        [DataRow("/bff/admin/authentication/options")]
        public async Task AdminCeremonyEndpoint_MoreRequestsThanTheLimit_AreRefusedWith429(string path)
        {
            var statuses = await SendManyAsync(() => AdminPost(path), Limit + 1);

            statuses.Take(Limit).Should().NotContain(HttpStatusCode.TooManyRequests);
            statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
        }

        private sealed class RemoteAddressFilter(string address) : Microsoft.AspNetCore.Hosting.IStartupFilter
        {
            public Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next) => app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(address);
                    return nextMiddleware();
                });
                next(app);
            };
        }

        [TestMethod]
        public async Task SignInEndpoint_BehindATrustedProxy_LimitsEachForwardedClientAddressOnItsOwn()
        {
            using var factory = new BffFactory().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Session:SignInRequestsPerMinute", Limit.ToString());
                builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.0.0.5");
                builder.ConfigureTestServices(services => services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter>(new RemoteAddressFilter("10.0.0.5")));
            });
            using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            async Task<HttpStatusCode> LoginFromAsync(string clientAddress)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/bff/login");
                request.Headers.Add("X-Forwarded-For", clientAddress);
                using var response = await client.SendAsync(request, TestContext.CancellationToken);
                return response.StatusCode;
            }

            for (var i = 0; i < Limit; i++)
            {
                await LoginFromAsync("203.0.113.1");
            }

            (await LoginFromAsync("203.0.113.1")).Should().Be(HttpStatusCode.TooManyRequests);
            (await LoginFromAsync("203.0.113.2")).Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        [TestMethod]
        public async Task SignInEndpoint_RefusedRequest_HasAProblemDetailsBody()
        {
            await SendManyAsync(() => new HttpRequestMessage(HttpMethod.Get, "/bff/login"), Limit);

            using var refused = await _browser.GetAsync("/bff/login", TestContext.CancellationToken);

            refused.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            (await refused.Content.ReadAsStringAsync(TestContext.CancellationToken)).Should().Contain("429");
        }

        [TestMethod]
        public async Task SessionEndpoint_ManyRequests_AreNotLimited()
        {
            var statuses = await SendManyAsync(() => new HttpRequestMessage(HttpMethod.Get, "/bff/session"), Limit * 3);

            statuses.Should().NotContain(HttpStatusCode.TooManyRequests);
        }

        private static HttpRequestMessage AdminPost(string path) => new(HttpMethod.Post, path)
        {
            Headers = { { "X-Requested-With", "fetch" } },
            Content = JsonContent.Create(new { bootstrapSecret = "secret" })
        };
    }
}