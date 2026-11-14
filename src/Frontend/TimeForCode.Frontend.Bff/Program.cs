using TimeForCode.Frontend.Bff;
using TimeForCode.Shared.Api.Extensions;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(LocationRewrite.Add)
    .AddTransforms(ForwardOnlyCookie.Add);
builder.Services.AddSingleton<IForwarderHttpClientFactory, BearerTokenForwarderHttpClientFactory>();

var app = builder.Build();

// First, so the scheme and host that HSTS and the Secure cookies see are the original ones behind a proxy.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseBrowserSecurityHeaders();
app.UseCsrfHeaderCheck();
app.MapReverseProxy();
app.MapSpaFiles();

await app.RunAsync();