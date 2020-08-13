using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Managers;

namespace Kuriimu2.Cmd.Contexts
{
    class TextContext : BaseContext
    {
        private readonly IStateInfo _stateInfo;
        private readonly IContext _parentContext;

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("back")
        };

        public TextContext(IStateInfo stateInfo, IContext parentContext)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            _stateInfo = stateInfo;
            _parentContext = parentContext;
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
