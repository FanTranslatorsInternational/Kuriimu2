using System.Threading.Tasks;
using Kuriimu2.Cmd.Parsers;

namespace Kuriimu2.Cmd.Contexts
{
    interface IContext
    {
        void PrintCommands();

        Task<IContext?> ExecuteNext(IArgumentGetter argumentGetter);
    }
}
