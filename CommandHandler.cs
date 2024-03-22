using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using FissionRevamped.Services;

namespace FissionRevamped
{
    public class CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services)
    {
        private char _prefix = '!';

        public async Task InstallCommandsAsync()
        {
            client.MessageReceived += HandleCommandAsync;

            var configLabel = Environment.GetEnvironmentVariable("FISSION_CONFIG") ?? "production";
            var prefixEnvVariable = configLabel.Equals("testing", StringComparison.OrdinalIgnoreCase)
                ? "FISSION_TESTING_PREFIX"
                : "FISSION_PREFIX";
            _prefix = Environment.GetEnvironmentVariable(prefixEnvVariable)?[0] ?? '!';

            LoggingService.LogInfo($"Using prefix '{_prefix}'.");

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (messageParam is not SocketUserMessage message)
                return;

            var argPos = 0;

            if (!(message.HasCharPrefix(_prefix, ref argPos) ||
                  message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var context = new SocketCommandContext(client, message);

            await commands.ExecuteAsync(
                context,
                argPos,
                services);
        }
    }
}
