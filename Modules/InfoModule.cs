using Discord;
using Discord.Commands;

namespace FissionRevamped.Modules
{
    public class InfoModule(IServiceProvider services) : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        [Summary("Echos a message.")]
        public Task SayAsync([Remainder][Summary("The text to echo")] string echo)
        {
            return ReplyAsync(echo, messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("tts")]
        public Task TtsAsync([Remainder] string msg)
        {
            return ReplyAsync(msg, true, messageReference: new MessageReference(Context.Message.Id));
        }

    }
}
