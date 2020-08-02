using System;
using System.Timers;
using Kontract.Interfaces.Progress;
using Kuriimu2.WinForms.Controls;

namespace Kuriimu2.WinForms.Progress
{
    class ToolStripProgressBarOutput : IProgressOutput
    {
        private readonly Kuriimu2ProgressBarToolStrip _progressBar;
        private readonly Timer _timer;

        private ProgressState _progressState;

        public ToolStripProgressBarOutput(Kuriimu2ProgressBarToolStrip progressBar, double updateInterval)
        {
            _progressBar = progressBar;

            _timer = new Timer(updateInterval);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Enabled = true;
        }

        public void SetProgress(ProgressState state)
        {
            _progressState = state;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_progressState == null)
                return;

            var localProgress = _progressState;

            var percentageValue = localProgress.PartialValue / (double)localProgress.MaxValue;
            var percentageInRange = (localProgress.MaxPercentage - localProgress.MinPercentage) * percentageValue;

            var message = string.IsNullOrWhiteSpace(localProgress.PreText) ?
                localProgress.Message :
                localProgress.PreText + localProgress.Message;
            message = string.IsNullOrWhiteSpace(message) ? string.Empty : message + " - ";

            var completion = localProgress.MinPercentage + percentageInRange;

            _progressBar.Value = Convert.ToInt32(completion);
            _progressBar.Text = message + $@"{completion:0.00}%";
        }
    }
}
