using System.Collections.Concurrent;
using System.Timers;
using Kontract;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;

namespace Kore.Logging
{
    public class ConcurrentLogger : IConcurrentLogger
    {
        private readonly ILogOutput _output;
        private readonly ApplicationLevel _applicationLevel;

        private readonly Timer _timer;
        private readonly ConcurrentQueue<(LogLevel, string)> _queue;

        public ConcurrentLogger(ApplicationLevel applicationLevel, ILogOutput output)
        {
            ContractAssertions.IsNotNull(output, nameof(output));

            _output = output;
            _applicationLevel = applicationLevel;

            _timer = new Timer(1000);
            _timer.Elapsed += Timer_Elapsed;
            _queue = new ConcurrentQueue<(LogLevel, string)>();
        }

        public void StartLogging()
        {
            _timer.Start();
        }

        public void StopLogging()
        {
            _timer.Stop();
            DumpQueue();
        }

        public void QueueMessage(LogLevel level, string message)
        {
            _queue.Enqueue((level, message));
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DumpQueue();
        }

        private void DumpQueue()
        {
            while (_queue.TryDequeue(out var element))
            {
                _output.Log(_applicationLevel, element.Item1, element.Item2);
            }
        }
    }
}
