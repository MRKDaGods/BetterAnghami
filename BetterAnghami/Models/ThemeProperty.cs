namespace MRK.Models
{
    public class ThemeProperty(string name, string value)
    {
        public string Name { get; init; } = name;
        public string Value { get; set; } = value;
    }
}
