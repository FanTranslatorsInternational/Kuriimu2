using System.Threading.Tasks;
using Kuriimu2.CommandLine.Parsers;

namespace Kuriimu2.CommandLine.Contexts
{
    interface IContext
    {
        void PrintCommands();

        Task<IContext> ExecuteNext(IArgumentGetter argumentGetter);
    }
}
