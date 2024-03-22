using System.Text;
using Discord;
using Discord.Audio;
using Discord.Commands;
using FissionRevamped.Extensions;
using FissionRevamped.Services.Music;
using Microsoft.Extensions.DependencyInjection;

namespace FissionRevamped.Modules
{
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        private IServiceProvider _services;

        public MusicModule(IServiceProvider services)
        {
            _services = services;
        }

        private async Task<IAudioClient?> ConnectToChannelAsync()
        {
            var guildMusicPlayer = _services.GetRequiredService<PlayerService>().GetMusicPlayer(Context.Guild.Id);
            var channel = (Context.User as IGuildUser)?.VoiceChannel;

            if (channel == null)
            {
                await this.SendErrorEmbedAsync("You must join a voice channel to use this command.");
                guildMusicPlayer.AudioClient = null;
                return null;
            }

            if (guildMusicPlayer.CurrentVoiceChannel != channel)
            {
                guildMusicPlayer.AudioClient = await channel.ConnectAsync(true);
                guildMusicPlayer.CurrentVoiceChannel = channel;
                guildMusicPlayer.CurrentMessageChannel = Context.Channel;
            }
            return guildMusicPlayer.AudioClient;
        }

        #region Connect

        [Command("connect", RunMode = RunMode.Async)]
        public async Task ConnectAsync()
        {
            var client = await ConnectToChannelAsync();
            if (client != null)
            {
                await this.SendInfoEmbedAsync("Connected!", messageReference: Context.Message.Id);
                await this.ReplySuccessReactionAsync();
            }
            else
            {
                await this.ReplyFailReactionAsync();
            }
        }

        #endregion

        #region Play

        [Command("play", Aliases = ["queue", "p"], RunMode = RunMode.Async)]
        public async Task PlayAsync([Remainder] string searchQuery)
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);
            var audioClient = await ConnectToChannelAsync();
            if (audioClient == null)
            {
                await this.ReplyFailReactionAsync();
                return;
            }

            // search track
            var message = await this.SendInfoEmbedAsync("Searching track...", "Music Player", Context.Message.Id);
            var track = playerService.SearchTrack(searchQuery, Context.User.Id, out var duration);
            var embedBuilder = new EmbedBuilder();
            if (track == null)
            {
                embedBuilder.WithAuthor("Fission - Music Player");
                embedBuilder.Description = "No suitable tracks found.";
                embedBuilder.WithFooter($"Search duration: {duration.TotalSeconds}s");
                await guildMusicPlayer.CurrentMessageChannel?.ModifyMessageAsync(message.Id, messageProps =>
                {
                    messageProps.Embed = embedBuilder.Build();
                })!;
                await this.ReplyFailReactionAsync();
                return;
            }
            if (guildMusicPlayer.Tracks.Exists(trackOnList => trackOnList.Id == track.Value.Id))
            {
                embedBuilder.WithAuthor("Fission - Music Player");
                embedBuilder.Description = "Track is already in queue.";
                embedBuilder.WithFooter($"Search duration: {duration.TotalSeconds}s");
                await guildMusicPlayer.CurrentMessageChannel?.ModifyMessageAsync(message.Id, messageProps =>
                {
                    messageProps.Embed = embedBuilder.Build();
                })!;
                await this.ReplyFailReactionAsync();
                return;
            }

            // enqueue track
            guildMusicPlayer.Tracks.Add(track.Value);
            var index = guildMusicPlayer.Tracks.IndexOf(track.Value);
            embedBuilder.WithAuthor("Fission - Music Player");
            embedBuilder.Description = $"""
                                        Position: `{index+1}`
                                        Track: `{track.Value.Title}`
                                        Duration: `{track.Value.Duration}`
                                        Queued by: <@{track.Value.EnqueuedBy}>
                                        """;
            embedBuilder.WithFooter($"Search duration: {duration.TotalSeconds}s");
            var embed = embedBuilder.Build();
            await guildMusicPlayer.CurrentMessageChannel?.ModifyMessageAsync(message.Id, messageProps =>
            {
                messageProps.Embed = embed;
            })!;

            // play if player is stopped
            guildMusicPlayer.PrepareNext();
            await this.ReplySuccessReactionAsync();
        }

        #endregion

        #region Link

        [Command("link", RunMode = RunMode.Async)]
        public async Task LinkAsync([Remainder] string link)
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);
            var audioClient = await ConnectToChannelAsync();
            if (audioClient == null)
            {
                await this.ReplyFailReactionAsync();
                return;
            }

            var message = await this.SendInfoEmbedAsync("Fetching track...", "Music Player", Context.Message.Id);

            // fetch track
            var track = playerService.FetchTrack(link, Context.User.Id, out var duration);
            var embedBuilder = new EmbedBuilder();
            if (track == null)
            {
                embedBuilder.WithAuthor("Fission - Music Player");
                embedBuilder.Description = "No suitable tracks found.";
                embedBuilder.WithFooter($"Search duration: {duration.TotalSeconds}s");
                await guildMusicPlayer.CurrentMessageChannel?.ModifyMessageAsync(message.Id, messageProps =>
                {
                    messageProps.Embed = embedBuilder.Build();
                })!;
                await this.ReplyFailReactionAsync();
                return;
            }
            if (guildMusicPlayer.Tracks.Exists(trackOnList => trackOnList.Id == track.Value.Id))
            {
                embedBuilder.WithAuthor("Fission - Music Player");
                embedBuilder.Description = "Track is already in queue.";
                embedBuilder.WithFooter($"Search duration: {duration.TotalSeconds}s");
                await guildMusicPlayer.CurrentMessageChannel?.ModifyMessageAsync(message.Id, messageProps =>
                {
                    messageProps.Embed = embedBuilder.Build();
                })!;
                await this.ReplyFailReactionAsync();
                return;
            }
            guildMusicPlayer.Tracks.Add(track.Value);

            // enqueue track
            var index = guildMusicPlayer.Tracks.IndexOf(track.Value);
            embedBuilder.WithAuthor("Fission - Music Player");
            embedBuilder.Description = $"""
                                        Position: `{index + 1}`
                                        Track: `{track.Value.Title}`
                                        Duration: `{track.Value.Duration}`
                                        Queued by: <@{track.Value.EnqueuedBy}>
                                        """;
            embedBuilder.WithFooter($"Search duration: {duration.TotalSeconds}s");
            var embed = embedBuilder.Build();
            await guildMusicPlayer.CurrentMessageChannel?.ModifyMessageAsync(message.Id, messageProps =>
            {
                messageProps.Embed = embed;
            })!;

            // play if player is stopped
            guildMusicPlayer.PrepareNext();
            await this.ReplySuccessReactionAsync();
        }

        #endregion

        #region Next

        [Command("next", Aliases = ["skip"], RunMode = RunMode.Async)]
        public async  Task NextAsync()
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);

            guildMusicPlayer.SkipToNext();
            await this.ReplySuccessReactionAsync();
        }

        #endregion

        #region Skip

        [Command("skipto", Aliases = ["skip"], RunMode = RunMode.Async)]
        public async Task SkipToAsync(int index)
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);
            if (index >= 0 && index < guildMusicPlayer.Tracks.Count)
            {
                await this.ReplyFailReactionAsync();
                return;
            }

            guildMusicPlayer.SkipToIndex(index);
            await this.ReplySuccessReactionAsync();
        }

        #endregion

        #region Jump

        [Command("jump", RunMode = RunMode.Async)]
        public async Task JumpAsync(int index)
        {
            await this.ReplySuccessReactionAsync();
        }

        #endregion

        #region Stop

        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopAsync()
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);

            guildMusicPlayer.Stop();
            await this.ReplySuccessReactionAsync();
        }

        #endregion

        #region Repeat

        [Command("repeat", RunMode = RunMode.Async)]
        public async Task RepeatAsync()
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);
            await this.ReplySuccessReactionAsync();

            guildMusicPlayer.Repeat = !guildMusicPlayer.Repeat;

            await this.SendInfoEmbedAsync(guildMusicPlayer.Repeat
                ? "Repeat track? \u2705"
                : "Repeat track? \u274c",
                "Music Player",
                Context.Message.Id);
        }

        #endregion

        #region Loop

        [Command("loop", RunMode = RunMode.Async)]
        public async Task LoopAsync()
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);
            await this.ReplySuccessReactionAsync();

            guildMusicPlayer.Loop = !guildMusicPlayer.Loop;

            await this.SendInfoEmbedAsync(guildMusicPlayer.Loop
                ? "Loop queue? \u2705"
                : "Loop queue? \u274c",
                "Music Player",
                Context.Message.Id);
        }

        #endregion

        #region NowPlaying

        [Command("nowplaying", Aliases = ["np", "song"], RunMode = RunMode.Async)]
        public async Task NowPlaying()
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);

            PlayerTrack? currentTrack = guildMusicPlayer.CurrentTrack >= 0
                                        && guildMusicPlayer.CurrentTrack < guildMusicPlayer.Tracks.Count
                ? guildMusicPlayer.Tracks[guildMusicPlayer.CurrentTrack]
                : null;
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithAuthor("Fission - Music Player");
            if (currentTrack == null)
            {
                embedBuilder.Description = "No tracks are playing.";
            }
            else
            {
                var descriptionBuilder = new StringBuilder();
                descriptionBuilder.AppendLine($"Track: {currentTrack.Value.Title}");
                descriptionBuilder.AppendLine($"{guildMusicPlayer.Position} - {guildMusicPlayer.Duration}");
                embedBuilder.Description = descriptionBuilder.ToString();
            }

            await ReplyAsync(embed: embedBuilder.Build(), messageReference: new MessageReference(Context.Message.Id));
            await this.ReplySuccessReactionAsync();
        }

        #endregion

        #region Queue

        [Command("queue", Aliases = ["list"], RunMode = RunMode.Async)]
        public async Task QueueAsync()
        {
            var playerService = _services.GetRequiredService<PlayerService>();
            var guildMusicPlayer = playerService.GetMusicPlayer(Context.Guild.Id);

            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithAuthor("Fission - Music Player");
            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.AppendLine($"{Context.Guild.Name}'s music player queue:");
            descriptionBuilder.AppendLine("```");
            if (guildMusicPlayer.Tracks.Count != 0)
            {
                var copy = new List<PlayerTrack>(guildMusicPlayer.Tracks);
                for (var i = 0; i < copy.Count; ++i)
                {
                    var maxLength = copy.Count.ToString().Length;
                    descriptionBuilder.Append(guildMusicPlayer.CurrentTrack == i ? "> " : "  ");
                    descriptionBuilder.Append('[');
                    var num = (i + 1).ToString();
                    descriptionBuilder.Append(num);
                    for (var j = num.Length; j < maxLength; ++j)
                        descriptionBuilder.Append(' ');
                    descriptionBuilder.AppendLine($"] {copy[i].Title.Truncate(28)} ({copy[i].Duration})");
                }
            }
            else
            {
                descriptionBuilder.AppendLine("No tracks found.");
            }
            descriptionBuilder.AppendLine("```");
            embedBuilder.Description = descriptionBuilder.ToString();
            embedBuilder.WithFooter($"Total of {guildMusicPlayer.Tracks.Count} track(s).");
            var embed = embedBuilder.Build();
            await ReplyAsync(embed: embed, messageReference: new MessageReference(Context.Message.Id));
            await this.ReplySuccessReactionAsync();
        }


        #endregion
    }
}
