using System.Collections.Generic;
using System.Threading.Tasks;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Progress;
using Kuriimu2.Cmd.Models.Contexts;

namespace Kuriimu2.Cmd.Contexts
{
    class FontContext(IFileState stateInfo, IContext parentContext, IProgressContext progressContext)
        : BaseContext(progressContext)
    {
        protected override Command[] GetCommandsInternal()
        {
            return
            [
                new Command("back")
            ];
        }

        protected override Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "back":
                    return Task.FromResult<IContext?>(parentContext);
            }

            return Task.FromResult<IContext?>(null);
        }
    }
}
