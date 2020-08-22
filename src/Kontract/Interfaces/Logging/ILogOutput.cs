using Kontract.Models.Logging;

namespace Kontract.Interfaces.Logging
{
    public interface ILogOutput
    {
        void Log(ApplicationLevel applicationLevel, LogLevel level, string message);
    }
}
