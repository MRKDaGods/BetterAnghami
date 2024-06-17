namespace MRK.Models
{
    /// <summary>
    /// Theme Metadata
    /// </summary>
    public class ThemeMetadata(string id, string name, string owner, string description, string version)
    {
        public string Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string Owner { get; init; } = owner;
        public string Description { get; init; } = description;
        public string Version { get; init; } = version;

        public override bool Equals(object? obj)
        {
            return obj is ThemeMetadata theme &&
                theme.Id == Id &&
                theme.Version == Version;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Name.GetHashCode() ^ Owner.GetHashCode() ^ Description.GetHashCode() ^ Version.GetHashCode();
        }
    }
}
