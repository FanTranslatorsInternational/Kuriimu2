using System;
using Kore.Progress;
using Kuriimu2.EtoForms.Controls;

namespace Kuriimu2.EtoForms.Progress
{
    class Kuriimu2ProgressBarOutput : BaseConcurrentProgressOutput
    {
        private readonly Kuriimu2ProgressBar _progressBar;

        public Kuriimu2ProgressBarOutput(Kuriimu2ProgressBar progressBar, int updateInterval) : base(updateInterval)
        {
            _progressBar = progressBar;
        }

        protected override void OutputProgressInternal(double completion, string message)
        {
            _progressBar.Value = Convert.ToInt32(completion);
            _progressBar.Text = message + $@" - {completion:0.00}%";
        }
    }
}
