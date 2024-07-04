using System;

namespace MRK.Models
{
    public class Song(int id, string name, string artist, string imgUrl)
    {
        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string Artist { get; init; } = artist;
        public string ImgUrl { get; init; } = imgUrl;

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
    }
}
