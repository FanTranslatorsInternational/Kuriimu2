using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Konnect.Contract.Progress;
using Kuriimu2.Cmd.Models.Contexts;
using Kuriimu2.Cmd.Parsers;

namespace Kuriimu2.Cmd.Contexts
{
    abstract class BaseContext(IProgressContext progressContext) : IContext
    {
        private Command[]? _commands;

        protected IProgressContext Progress { get; } = progressContext;

        public void PrintCommands()
        {
            Console.WriteLine();
            Console.WriteLine("Available commands:");

            Command[] commands = GetCommands();
            foreach (Command command in commands.Where(x => x.Enabled))
                Console.WriteLine($"{command.Name} {string.Join(' ', command.Arguments.Select(x => $"[{x}]"))}");
        }

        public async Task<IContext?> ExecuteNext(IArgumentGetter argumentGetter)
        {
            string commandName = argumentGetter.GetNextArgument();

            // Check if command exists
            Command[] commands = GetCommands();
            Command? command = commands.FirstOrDefault(x => x.Enabled && x.Name == commandName);
            if (command is null)
            {
                Console.WriteLine($"Command '{commandName}' is not supported.");
                return this;
            }

            Console.Clear();

            // Execute command
            IList<string> arguments = argumentGetter.GetNextArguments(command.Arguments.Length);
            return await ExecuteNextInternal(command, arguments);
        }

        private Command[] GetCommands() => _commands ??= GetCommandsInternal();

        protected virtual Command[] GetCommandsInternal() => [];

        protected abstract Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments);
    }
}
