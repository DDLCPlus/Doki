namespace Doki.RenpyUtils
{
    public class RenpyDefinition(string name, string value)
    {
        public string Name { get; set; } = name;
        public string Value { get; set; } = value;
    }
}
