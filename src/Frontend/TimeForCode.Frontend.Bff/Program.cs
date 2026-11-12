using TimeForCode.Frontend.Bff;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(LocationRewrite.Add);
builder.Services.AddSingleton<IForwarderHttpClientFactory, BearerTokenForwarderHttpClientFactory>();
builder.Services.AddSessionEndpoint(builder.Configuration);

var app = builder.Build();

app.UseBrowserSecurityHeaders();
app.UseCsrfHeaderCheck();
app.MapSessionEndpoint();
app.MapReverseProxy();
app.MapSpaFiles();

await app.RunAsync();