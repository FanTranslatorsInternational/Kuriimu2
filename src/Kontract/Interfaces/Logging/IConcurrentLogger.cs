using Kontract.Models.Logging;

namespace Kontract.Interfaces.Logging
{
    public interface IConcurrentLogger
    {
        void StartLogging();

        bool IsRunning();

        void StopLogging();

        void QueueMessage(LogLevel level, string message);
    }
}
