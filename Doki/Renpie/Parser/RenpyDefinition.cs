namespace Doki.Renpie.Parser
{
    public enum DefinitionType
    {
        Define,
        Image,
        Character,
        Unknown,
        Audio,
        Variable
    }

    public class RenpyDefinition(string name, string value, DefinitionType type)
    {
        public string Name { get; set; } = name;
        public string Value { get; set; } = value;
        public DefinitionType Type { get; set; } = type;
    }
}
