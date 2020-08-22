using Kontract;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;

namespace Kore.Logging
{
    public class ConcurrentLogger : IConcurrentLogger
    {
        private readonly ILogOutput _output;
        private readonly ApplicationLevel _applicationLevel;

        private bool _enqueueMessage;

        public ConcurrentLogger(ApplicationLevel applicationLevel, ILogOutput output)
        {
            ContractAssertions.IsNotNull(output, nameof(output));

            _output = output;
            _applicationLevel = applicationLevel;

            _enqueueMessage = false;
        }

        public void StartLogging()
        {
            _enqueueMessage = true;
        }

        public void StopLogging()
        {
            _enqueueMessage = false;
        }

        public void QueueMessage(LogLevel level, string message)
        {
            if (_enqueueMessage)
                _output.Log(_applicationLevel, level, message);
        }
    }
}
