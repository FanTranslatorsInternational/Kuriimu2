using System.Collections.Generic;

namespace Kuriimu2.CommandLine.Parsers
{
    interface IArgumentGetter
    {
        string GetNextArgument();
        IList<string> GetNextArguments(int count);
    }
}
