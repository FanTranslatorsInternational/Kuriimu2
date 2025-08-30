namespace Kuriimu2.Cmd.Models.Contexts
{
    class Command(string name, params string[] argumentNames)
    {
        public string Name { get; } = name;

        public string[] Arguments { get; } = argumentNames;

        public bool Enabled { get; set; } = true;
    }
}
