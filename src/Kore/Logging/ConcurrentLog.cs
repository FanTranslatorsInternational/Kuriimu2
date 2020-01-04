using System.Collections.Concurrent;
using System.Timers;
using Kontract;

namespace Kore.Logging
{
    public class ConcurrentLog : IConcurrentLog
    {
        private readonly ILogOutput _output;

        private readonly Timer _timer;
        private ConcurrentQueue<(LogLevel, string)> _queue;

        public ConcurrentLog(ILogOutput output)
        {
            ContractAssertions.IsNotNull(output, nameof(output));

            _output = output;

            _timer = new Timer(500);
            _timer.Elapsed += Timer_Elapsed;
            _queue = new ConcurrentQueue<(LogLevel, string)>();
        }

        public void StartOutput()
        {
            _timer.Start();
        }

        public void StopOutput()
        {
            _timer.Stop();
            DumpQueue();
        }

        public void ResetLog()
        {
            _queue = new ConcurrentQueue<(LogLevel, string)>();
            _output.Clear();
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
                _output.LogLine(element.Item1, element.Item2);
            }
        }
    }
}
