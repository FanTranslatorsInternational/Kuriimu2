using System;
using ImGui.Forms.Controls;
using Kore.Progress;

namespace Kuriimu2.ImGui.Progress
{
    class ProgressBarOutput : BaseConcurrentProgressOutput
    {
        private readonly ProgressBar _progressBar;

        public ProgressBarOutput(ProgressBar progressBar,int updateInterval) : base(updateInterval)
        {
            _progressBar = progressBar;
        }

        protected override void OutputProgressInternal(double completion, string message)
        {
            if (_progressBar == null)
                return;

            _progressBar.Value = Convert.ToInt32(completion);
            _progressBar.Text = message + $@" - {completion:0.00}%";
        }
    }
}
