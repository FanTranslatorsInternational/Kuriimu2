using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;

namespace Kuriimu2.Cmd.Contexts
{
    class TextContext : BaseContext
    {
        private readonly IFileState _stateInfo;
        private readonly IContext _parentContext;

        public TextContext(IFileState stateInfo, IContext parentContext, IProgressContext progressContext) :
            base(progressContext)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            _stateInfo = stateInfo;
            _parentContext = parentContext;
        }

        protected override IList<Command> InitializeCommands()
        {
            return new[]
            {
                new Command("back")
            };
        }

        protected override Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "back":
                    return Task.FromResult(_parentContext);
            }

            return null;
        }
    }
}
