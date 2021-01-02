using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;

namespace Kore.Logging
{
    public class NullLogOutput:ILogOutput
    {
        public void Log(ApplicationLevel applicationLevel, LogLevel level, string message)
        {
        }
    }
}
