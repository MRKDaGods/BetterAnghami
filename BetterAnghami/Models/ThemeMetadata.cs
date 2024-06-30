namespace MRK.Models
{
    /// <summary>
    /// Theme Metadata
    /// </summary>
    public class ThemeMetadata(string id, string name, int creatorId, string creatorName, string description, string version, bool isBuiltIn = false)
    {
        public string Id { get; init; } = id;
        public string Name { get; init; } = name;
        public int CreatorId { get; init; } = creatorId;
        public string CreatorName { get; init; } = creatorName;
        public string Description { get; init; } = description;
        public string Version { get; init; } = version;
        public bool IsBuiltIn { get; init; } = isBuiltIn;

        public override bool Equals(object? obj)
        {
            return obj is ThemeMetadata theme &&
                theme.Id == Id &&
                theme.Version == Version;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Name.GetHashCode() ^ CreatorId.GetHashCode() ^ Description.GetHashCode() ^ Version.GetHashCode();
        }
    }
}
