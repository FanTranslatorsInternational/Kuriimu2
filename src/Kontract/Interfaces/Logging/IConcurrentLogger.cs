using Kontract.Models.Logging;

namespace Kontract.Interfaces.Logging
{
    public interface IConcurrentLogger
    {
        void StartLogging();

        void StopLogging();

        void QueueMessage(LogLevel level, string message);
    }
}
