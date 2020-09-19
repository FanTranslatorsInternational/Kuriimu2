using Kontract;

namespace Kuriimu2.Cmd.Contexts
{
    class Command
    {
        public string Name { get; }

        public string[] Arguments { get; }

        public Command(string name,params string[] argumentNames)
        {
            ContractAssertions.IsNotNull(name,nameof(name));
            ContractAssertions.IsNotNull(argumentNames,nameof(argumentNames));

            Name = name;
            Arguments = argumentNames;
        }
    }
}
