using System;
using System.Text.RegularExpressions;

namespace MRK.Models
{
    public enum SongPlayStatus
    {
        Unknown,
        Paused,
        Playing,
        Buffering
    }

    public partial class Song(int id, string name, string artist, string imgUrl, string playState, string durStart, string durEnd)
    {
        public static class PlayStateNames
        {
            public const string Play = "play";
            public const string Pause = "pause";
            public const string Buffering = "buffering";
        }

        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string Artist { get; init; } = artist;
        public string ImgUrl { get; init; } = imgUrl;
        public string PlayState { get; init; } = playState;
        public string DurStart { get; init; } = durStart;
        public string DurEnd { get; init; } = durEnd;

        /// <summary>
        /// Elapsed song time in seconds
        /// </summary>
        public int ElapsedTime => GetTimeFromDuration(DurStart);

        /// <summary>
        /// Remaining song time in seconds
        /// </summary>
        public int RemainingTime => GetTimeFromDuration(DurEnd);

        /// <summary>
        /// Current song playing status
        /// </summary>
        public SongPlayStatus SongPlayStatus
        {
            get
            {
                switch (PlayState)
                {
                    // if pause button visible, we are playing
                    case PlayStateNames.Pause:
                        return SongPlayStatus.Playing;

                    // if play button visible, we are paused
                    case PlayStateNames.Play:
                        return SongPlayStatus.Paused;

                    // buffer as it is
                    case PlayStateNames.Buffering:
                        return SongPlayStatus.Buffering;
                }

                return SongPlayStatus.Unknown;
            }
        }

        /// <summary>
        /// Is the song currently playing?
        /// </summary>
        public bool IsPlaying => SongPlayStatus == SongPlayStatus.Playing;

        private static int GetTimeFromDuration(string dur)
        {
            // 00:00
            var match = DurationRegex().Match(dur);
            if (match.Success)
            {
                var minutes = int.Parse(match.Groups[1].ValueSpan);
                var secs = int.Parse(match.Groups[3].ValueSpan);

                return Math.Max(minutes * 60 + secs, 0);
            }

            return 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Song other)
            {
                return Id == other.Id &&
                       Name == other.Name &&
                       Artist == other.Artist &&
                       ImgUrl == other.ImgUrl;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Artist, ImgUrl);
        }

        public static bool operator ==(Song? left, Song? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Song? left, Song? right)
        {
            return !(left == right);
        }

        [GeneratedRegex(@"(\d+)(:)(\d+)")]
        private static partial Regex DurationRegex();
    }
}
