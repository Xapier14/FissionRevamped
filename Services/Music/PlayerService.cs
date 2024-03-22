using System.Diagnostics;

namespace FissionRevamped.Services.Music
{
    public class PlayerService
    {
        private readonly Dictionary<ulong, GuildMusicPlayer> _playerStates = [];

        public GuildMusicPlayer GetMusicPlayer(ulong guildId)
        {
            if (!_playerStates.TryGetValue(guildId, out var state))
            {
                state = new GuildMusicPlayer(this);
                _playerStates[guildId] = state;
            }

            return state;
        }

        public PlayerTrack? SearchTrack(string searchQuery, ulong enqueuedBy, out TimeSpan searchDuration)
        {
            var ytDlp = Process.Start(new ProcessStartInfo("yt-dlp")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = $"-f 140 --print urls --print id --print title --print channel --print duration_string --default-search ytsearch \"{searchQuery}\"",
            });
            if (ytDlp == null)
            {
                throw new Exception("Failed to create YT-DLP");
            }
            ytDlp.WaitForExit();
            var rawOutput = ytDlp.StandardOutput.ReadToEnd();
            var lines = rawOutput?.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? ["", "", ""];
            var url = lines[0];
            var id = lines[1];
            var title = lines[2];
            var channel = lines[3];
            var duration = lines[4];
            var unitCount = duration.Count(c => c == ':');
            for (var i = unitCount; i < 2; ++i)
                duration = "00:" + duration;
            searchDuration = ytDlp.ExitTime - ytDlp.StartTime;

            var track = new PlayerTrack
            {
                Artist = channel,
                Duration = TimeSpan.Parse(duration),
                EnqueuedBy = enqueuedBy,
                Id = id,
                Source = url,
                Title = title,
            };
            return track;
        }

        public PlayerTrack? FetchTrack(string youtubeUrl, ulong enqueuedBy, out TimeSpan searchDuration)
        {
            var ytDlp = Process.Start(new ProcessStartInfo("yt-dlp")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = $"-f 140 --print urls --print id --print title --print channel --print duration_string {youtubeUrl}",
            });
            if (ytDlp == null)
            {
                throw new Exception("Failed to create YT-DLP");
            }
            ytDlp.WaitForExit();
            var rawOutput = ytDlp.StandardOutput.ReadToEnd();
            var lines = rawOutput?.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? ["", "", ""];
            var url = lines[0];
            var id = lines[1];
            var title = lines[2];
            var channel = lines[3];
            var duration = lines[4];
            var unitCount = duration.Count(c => c == ':');
            for (var i = unitCount; i < 2; ++i)
                duration = "00:" + duration;
            searchDuration = ytDlp.ExitTime - ytDlp.StartTime;

            var track = new PlayerTrack
            {
                Artist = channel,
                Duration = TimeSpan.Parse(duration),
                EnqueuedBy = enqueuedBy,
                Id = id,
                Source = url,
                Title = title,
            };
            return track;
        }

        public Process CreateFfmpegProcess(string url, int volume)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo("ffmpeg")
                {
                    Arguments = $"-hide_banner -loglevel panic -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i \"{url}\" -ac 2 -f s16le -ar 48000 -",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                LoggingService.LogError(args.Data ?? "");
            };
            process.Start();
            return process;
        }
    }
}
