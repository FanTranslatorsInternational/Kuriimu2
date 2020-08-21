using System.Timers;
using Kontract.Interfaces.Progress;

namespace Kore.Progress
{
    public abstract class BaseConcurrentProgressOutput : IProgressOutput
    {
        private readonly Timer _timer;
        private ProgressState _progressState;

        private readonly object _lock = new object();
        private bool _isUpdating;

        public BaseConcurrentProgressOutput(int updateInterval)
        {
            _timer = new Timer(updateInterval);
            _timer.Elapsed += Timer_Elapsed;
        }

        public void SetProgress(ProgressState state)
        {
            _progressState = state;
        }

        public void StartProgress()
        {
            _timer.Start();
        }

        public void FinishProgress()
        {
            _timer.Stop();

            OutputProgress();
        }

        protected abstract void OutputProgressInternal(double completion, string message);

        private void OutputProgress()
        {
            lock (_lock)
            {
                if (_isUpdating || _progressState == null)
                    return;

                _isUpdating = true;
            }

            var localProgress = _progressState;

            var percentageValue = localProgress.PartialValue / (double)localProgress.MaxValue;
            var percentageInRange = (localProgress.MaxPercentage - localProgress.MinPercentage) * percentageValue;

            var message = string.IsNullOrWhiteSpace(localProgress.PreText) ?
                localProgress.Message :
                localProgress.PreText + localProgress.Message;
            message = string.IsNullOrWhiteSpace(message) ? string.Empty : message;

            var completion = localProgress.MinPercentage + percentageInRange;

            OutputProgressInternal(completion, message);

            _isUpdating = false;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OutputProgress();
        }
    }
}
