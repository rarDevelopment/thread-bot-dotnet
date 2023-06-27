global using Discord;
global using Discord.Interactions;
global using Discord.WebSocket;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;
using DiscordDotNetUtilities;
using DiscordDotNetUtilities.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Reflection;
using ThreadBot;

var builder = new HostBuilder();

builder.ConfigureAppConfiguration(options
    => options.AddJsonFile("appsettings.json")
        .AddUserSecrets(Assembly.GetEntryAssembly(), true)
        .AddEnvironmentVariables())
    .ConfigureHostConfiguration(configHost =>
    {
        configHost.AddEnvironmentVariables(prefix: "DOTNET_");
    });

var loggerConfig = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File($"logs/log-{DateTime.Now:dd.MM.yy_HH.mm}.log")
    .CreateLogger();

builder.ConfigureServices((host, services) =>
{
    services.AddLogging(options => options.AddSerilog(loggerConfig, dispose: true));
    services.AddSingleton(new DiscordSocketClient(
        new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            FormatUsersInBidirectionalUnicode = false,
            AlwaysDownloadUsers = true,
            LogGatewayIntentWarnings = false
        }));

    var discordSettings = new DiscordSettings(host.Configuration["Discord:BotToken"]!);
    var versionSettings = new VersionSettings(host.Configuration["Version:VersionNumber"]!);

    var databaseSettings = new DatabaseSettings(
        host.Configuration["Database:Cluster"]!,
        host.Configuration["Database:User"]!,
        host.Configuration["Database:Password"]!,
        host.Configuration["Database:Name"]!
    );

    services.AddSingleton(discordSettings);
    services.AddSingleton(versionSettings);
    services.AddSingleton(databaseSettings);

    services.AddScoped<IDiscordFormatter, DiscordFormatter>();

    services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));

    services.AddSingleton<InteractionHandler>();

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(DiscordBot).GetTypeInfo().Assembly));

    services.AddHostedService<DiscordBot>();
});

var app = builder.Build();

await app.RunAsync();
