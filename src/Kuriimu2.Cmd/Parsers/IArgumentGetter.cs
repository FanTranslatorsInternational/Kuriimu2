using System.Collections.Generic;

namespace Kuriimu2.Cmd.Parsers
{
    interface IArgumentGetter
    {
        string GetNextArgument();
        IList<string> GetNextArguments(int count);
    }
}
