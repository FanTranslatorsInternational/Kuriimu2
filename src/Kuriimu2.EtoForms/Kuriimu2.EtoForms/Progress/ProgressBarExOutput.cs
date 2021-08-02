using System;
using Eto.Forms;
using Kore.Progress;
using Kuriimu2.EtoForms.Controls;

namespace Kuriimu2.EtoForms.Progress
{
    class ProgressBarExOutput : BaseConcurrentProgressOutput
    {
        private readonly ProgressBarEx _progressBarEx;

        public ProgressBarExOutput(ProgressBarEx progressBarEx, int updateInterval) : base(updateInterval)
        {
            _progressBarEx = progressBarEx;
        }

        protected override void OutputProgressInternal(double completion, string message)
        {
            Application.Instance.Invoke(() =>
            {
                if (_progressBarEx == null) return;
                _progressBarEx.Value = Convert.ToInt32(completion);
                _progressBarEx.Text = message + $@" - {completion:0.00}%";
            });
        }
    }
}
