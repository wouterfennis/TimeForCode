using TimeForCode.Frontend.Bff;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(LocationRewrite.Add);
builder.Services.AddSingleton<IForwarderHttpClientFactory, BearerTokenForwarderHttpClientFactory>();

var app = builder.Build();

app.UseBrowserSecurityHeaders();
app.UseCsrfHeaderCheck();
app.MapReverseProxy();
app.MapSpaFiles();

await app.RunAsync();