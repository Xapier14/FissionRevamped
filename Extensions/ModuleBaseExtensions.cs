using Discord;
using Discord.Commands;
using Discord.Rest;

namespace FissionRevamped.Extensions
{
    public static class ModuleBaseExtensions
    {
        public static async Task ReplySuccessReactionAsync(this ModuleBase<SocketCommandContext> module)
            => await module.ReplyReactionAsync("\u2705");
        public static async Task ReplyFailReactionAsync(this ModuleBase<SocketCommandContext> module)
            => await module.ReplyReactionAsync("\u274c");

        public static async Task ReplyReactionAsync(this ModuleBase<SocketCommandContext> module, string emote)
        {
            var emoji = new Emoji(emote);
            await module.Context.Message.AddReactionAsync(emoji);
        }

        public static async Task<RestUserMessage> SendInfoEmbedAsync(this ModuleBase<SocketCommandContext> module, string message, string? title = null, ulong? messageReference = null)
        {
            var builder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = title == null ? "Fission" : $"Fission - {title}"
                },
                Description = message,
                Footer = new EmbedFooterBuilder
                {
                    Text = "Fission Revamped"
                }
            };

            return await module.Context.Channel.SendMessageAsync(embed: builder.Build(), messageReference: new MessageReference(messageReference));
        }

        public static async Task<RestUserMessage> SendErrorEmbedAsync(this ModuleBase<SocketCommandContext> module,
            string message, ulong? messageReference = null)
            => await SendInfoEmbedAsync(module, message, "Error", messageReference);
    }
}
