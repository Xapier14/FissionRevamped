using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using dotenv.net;

using FissionRevamped;
using FissionRevamped.Services;
using FissionRevamped.Services.Music;

DependencyHelper.TestDependencies();

// Load .env configuration
DotEnv.Load();
var configLabel = Environment.GetEnvironmentVariable("FISSION_CONFIG") ?? "production";
var tokenEnvVariable = configLabel.Equals("testing", StringComparison.OrdinalIgnoreCase)
    ? "FISSION_TESTING_TOKEN"
    : "FISSION_TOKEN";
var token = Environment.GetEnvironmentVariable(tokenEnvVariable);
Console.WriteLine("[*] Configuration is '{0}'.", configLabel.ToUpperInvariant());

// Ensure token exists
if (token == null)
{
    Console.Error.WriteLine("[*] No token ('{0}' env variable) provided.", tokenEnvVariable);
    Environment.Exit(-1);
}

// Setup service collection and Discord.NET client
await using var services = ConfigureServices();
var client = services.GetRequiredService<DiscordSocketClient>();
var commands = services.GetRequiredService<CommandService>();
client.Log += LoggingService.HandleLog;
client.Ready += OnReady;
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();
var commandHandler = new CommandHandler(client, commands, services);
await commandHandler.InstallCommandsAsync();
await Task.Delay(-1);
return;

ServiceProvider ConfigureServices()
{
    return new ServiceCollection()
        .AddSingleton(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        })
        .AddSingleton<DiscordSocketClient>()
        .AddSingleton<PlayerService>()
        .AddSingleton<CommandService>()
        .AddSingleton<HttpClient>()
        .BuildServiceProvider();
}

Task OnReady()
{
    LoggingService.LogInfo($"Bot is on {client.Guilds.Count} server(s).");
    return Task.CompletedTask;
}