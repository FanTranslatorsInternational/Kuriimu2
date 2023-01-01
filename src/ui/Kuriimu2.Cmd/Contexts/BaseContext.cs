using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Progress;
using Kuriimu2.Cmd.Parsers;

namespace Kuriimu2.Cmd.Contexts
{
    abstract class BaseContext : IContext
    {
        private readonly IList<Command> _commands;

        protected IProgressContext Progress { get; }

        protected BaseContext(IProgressContext progressContext)
        {
            ContractAssertions.IsNotNull(progressContext, nameof(progressContext));

            Progress = progressContext;

            _commands = InitializeCommands();
        }

        public void PrintCommands()
        {
            Console.WriteLine();
            Console.WriteLine("Available commands:");
            foreach (var command in _commands.Where(x => x.Enabled))
                Console.WriteLine($"{command.Name} {string.Join(' ', command.Arguments.Select(x => $"[{x}]"))}");
        }

        public async Task<IContext> ExecuteNext(IArgumentGetter argumentGetter)
        {
            var commandName = argumentGetter.GetNextArgument();

            // Check if command exists
            var command = _commands.Where(x => x.Enabled).FirstOrDefault(x => x.Name == commandName);
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

        protected abstract IList<Command> InitializeCommands();
    }
}
