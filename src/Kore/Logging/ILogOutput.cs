namespace Kore.Logging
{
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    }

    public interface ILogOutput
    {
        void LogLine(LogLevel level, string message);

        void Clear();
    }
}
