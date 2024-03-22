namespace FissionRevamped.Services.Music
{
    public struct PlayerTrack
    {
        public string Id;
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Source { get; set; }
        public TimeSpan Duration { get; set; }
        public ulong EnqueuedBy { get; set; }
    }
}
