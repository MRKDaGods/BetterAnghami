namespace MRK.Models
{
    /// <summary>
    /// Anghami user
    /// </summary>
    public class User(int id, string name)
    {
        /// <summary>
        /// Anghami ID
        /// </summary>
        public int Id { get; init; } = id;

        /// <summary>
        /// Full name
        /// </summary>
        public string Name { get; init; } = name;
    }
}
