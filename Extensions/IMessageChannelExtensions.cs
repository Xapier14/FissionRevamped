using Discord;
using System;

namespace FissionRevamped.Extensions
{
    public static class IMessageChannelExtensions
    {
        public static async Task<IUserMessage> SendInfoEmbedAsync(this IMessageChannel channel, string message, string? title = null, ulong? messageReference = null)
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

            return await channel.SendMessageAsync(embed: builder.Build(), messageReference: new MessageReference(messageReference));
        }


        public static async Task<IUserMessage> SendErrorEmbedAsync(this IMessageChannel channel,
            string message, ulong? messageReference = null)
            => await SendInfoEmbedAsync(channel, message, "Error", messageReference);
    }
}
