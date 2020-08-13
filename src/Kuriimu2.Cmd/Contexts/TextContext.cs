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
        private readonly IMainContext _mainContext;

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("back")
        };

        public TextContext(IStateInfo stateInfo, IContext parentContext, IMainContext mainContext)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));
            ContractAssertions.IsNotNull(mainContext, nameof(mainContext));

            _stateInfo = stateInfo;
            _parentContext = parentContext;
            _mainContext = mainContext;
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
