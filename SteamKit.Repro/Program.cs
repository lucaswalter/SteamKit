using SteamKit.Repro;
using SteamKit2;

var builder = WebApplication.CreateBuilder(args);

// Add Console Logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConsole();
});

// Add Hosted Service
builder.Services.AddHostedService<SteamService>();

var app = builder.Build();

// Add SteamKit2 Debug Logging
DebugLog.Enabled = true;
DebugLog.AddListener((category, msg) =>
{
    app.Services.GetRequiredService<ILogger<Program>>().LogInformation("[SteamKit] - {0}: {1}", category, msg);
});

app.Run();
