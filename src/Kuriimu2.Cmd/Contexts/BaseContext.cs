using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kuriimu2.CommandLine.Parsers;

namespace Kuriimu2.CommandLine.Contexts
{
    abstract class BaseContext : IContext
    {
        protected abstract IList<Command> Commands { get; }

        public void PrintCommands()
        {
            Console.WriteLine();
            Console.WriteLine("Available commands:");
            foreach (var command in Commands)
                Console.WriteLine($"{command.Name} {string.Join(' ', command.Arguments.Select(x => $"[{x}]"))}");
        }

        public async Task<IContext> ExecuteNext(IArgumentGetter argumentGetter)
        {
            var commandName = argumentGetter.GetNextArgument();

            // Check if command exists
            var command = Commands.FirstOrDefault(x => x.Name == commandName);
            if (command == null)
            {
                Console.WriteLine($"Command '{commandName}' is not supported.");
                return this;
            }

            Console.Clear();

            // Execute command
            var arguments = argumentGetter.GetNextArguments(command.Arguments.Length);
            return await ExecuteNextInternal(command, arguments);
        }

        protected abstract Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments);
    }
}
