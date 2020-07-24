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
            if (_progressState == null) return;

            var percentageValue = _progressState.PartialValue / (double)_progressState.MaxValue;
            var percentageInRange = (_progressState.MaxPercentage - _progressState.MinPercentage) * percentageValue;

            var message = string.IsNullOrWhiteSpace(_progressState.PreText) ?
                _progressState.Message :
                _progressState.PreText + _progressState.Message;
            message = string.IsNullOrWhiteSpace(message) ? string.Empty : message + " - ";

            var completion = _progressState.MinPercentage + percentageInRange;

            _progressBar.Value = Convert.ToInt32(completion);
            _progressBar.Text = message + $@"{completion:0.00}%";
        }
    }
}
