using Discord;
using Discord.Audio;

namespace FissionRevamped.Services.Music
{
    public class GuildMusicPlayer
    {
        private readonly PlayerService _playerService;
        private CancellationTokenSource? _cancellationTokenSource;
        private DateTime _streamStart = DateTime.Now;
        private DateTime? _streamPaused;
        private TimeSpan _streamOffset = TimeSpan.Zero;
        private ulong _lastNowPlayingUpdate = 0;

        public IAudioClient? AudioClient { get; set; }
        public IVoiceChannel? CurrentVoiceChannel { get; set; }
        public IMessageChannel? CurrentMessageChannel { get; set; }
        public PlayerState CurrentState { get; private set; }
        public List<PlayerTrack> Tracks { get; init; } = [];
        public int CurrentTrack { get; set; } = -1;
        public TimeSpan Position =>  (_streamPaused ?? DateTime.Now) - _streamStart + _streamOffset;
        public TimeSpan Duration => CurrentTrack >= 0 && CurrentTrack < Tracks.Count ? Tracks[CurrentTrack].Duration : TimeSpan.Zero;
        public bool Repeat { get; set; } = false;
        public bool Loop { get; set; } = false;

        public GuildMusicPlayer(PlayerService playerService)
        {
            _playerService = playerService;
            var playerThread = new Thread(PlayerThread_Work);
            playerThread.Start();
        }

        public void PrepareNext()
        {
            if (CurrentState == PlayerState.Stopped)
            {
                CurrentState = PlayerState.Preparing;
            }
        }

        public void Stop()
        {
            CurrentState = PlayerState.Stopped;
            CurrentTrack = 0;

            if (CurrentState == PlayerState.Playing)
            {
                _cancellationTokenSource?.Cancel();
            }
        }

        public void SkipToNext()
            => SkipToIndex(CurrentTrack + 1);

        public void SkipToIndex(int index)
        {
            CurrentTrack = index;
            CurrentState = PlayerState.Preparing;
            _cancellationTokenSource?.Cancel();
        }

        private async void PlayerThread_Work()
        {
            while (true)
            {
                try
                {
                    if (AudioClient == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (CurrentState == PlayerState.Preparing && Tracks.Count > 0)
                    {
                        // if queue has ended, start from end
                        if (CurrentTrack == -1)
                        {
                            CurrentTrack = Tracks.Count - 1;
                        }

                        // if queue is somehow greater than track count
                        if (CurrentTrack >= Tracks.Count)
                        {
                            // if loop (repeat all) is enabled, start from beginning
                            if (Loop)
                            {
                                CurrentTrack = 0;
                            }
                            else
                            {
                                // else, stop player
                                CurrentTrack = -1;
                                CurrentState = PlayerState.Stopped;
                                continue;
                            }
                        }

                        // play and stream music to discord
                        CurrentState = PlayerState.Playing;
                        var embedBuilder = new EmbedBuilder();
                        embedBuilder.WithAuthor("Fission - Music Player");
                        embedBuilder.Description = $"""
                                                    Now Playing: `{Tracks[CurrentTrack].Title}`
                                                    Enqueued by: <@{Tracks[CurrentTrack].EnqueuedBy}>
                                                    """;
                        embedBuilder.WithFooter($"Track {CurrentTrack + 1} of {Tracks.Count}");
                        var lastMessage = (await CurrentMessageChannel!.GetMessagesAsync(1).FirstAsync()).First();
                        if (lastMessage.Id == _lastNowPlayingUpdate)
                        {
                            CurrentMessageChannel?.ModifyMessageAsync(lastMessage.Id, messageProperties =>
                            {
                                messageProperties.Embed = embedBuilder.Build();
                            });
                        }
                        else
                        {
                            var message = await CurrentMessageChannel?.SendMessageAsync(embed: embedBuilder.Build())!;
                            _lastNowPlayingUpdate = message.Id;
                        }

                        await using var ffmpeg = _playerService.CreateFfmpegProcess(Tracks[CurrentTrack].Source, 100).StandardOutput.BaseStream;
                        await using var discord = AudioClient.CreatePCMStream(AudioApplication.Music, bufferMillis: 10000, packetLoss: 50);
                        _cancellationTokenSource = new CancellationTokenSource();
                        _streamStart = DateTime.Now;
                        try
                        {
                            await ffmpeg.CopyToAsync(discord, _cancellationTokenSource.Token);
                        }
                        finally
                        {
                            // wait for it to finish
                            if (!_cancellationTokenSource.Token.IsCancellationRequested)
                                await discord.FlushAsync(_cancellationTokenSource.Token);
                        }

                        _cancellationTokenSource = null;
                        // move to next track if still playing
                        if (CurrentState == PlayerState.Playing)
                        {
                            CurrentState = PlayerState.Preparing;
                            // move to next if not repeating
                            if (!Repeat)
                                CurrentTrack++;
                        }
                        continue;
                    }
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
