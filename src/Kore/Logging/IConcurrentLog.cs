namespace Kore.Logging
{
    public interface IConcurrentLog
    {
        void StartOutput();

        void StopOutput();

        void ResetLog();

        void QueueMessage(LogLevel level, string message);
    }
}
