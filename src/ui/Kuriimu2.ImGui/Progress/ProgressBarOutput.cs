using System;
using ImGui.Forms.Controls;
using ImGui.Forms.Localization;
using Konnect.Progress;

namespace Kuriimu2.ImGui.Progress
{
    internal class ProgressBarOutput(ProgressBar progressBar, int updateInterval, Func<double, LocalizedString> getMessage)
        : ConcurrentProgressOutput(updateInterval)
    {
        private Func<double, LocalizedString> _message = getMessage;

        public void SetMessage(Func<double, LocalizedString> message) => _message = message;

        protected override void OutputProgressInternal(double completion, string message)
        {
            progressBar.Value = Convert.ToInt32(completion);
            progressBar.Text = _message(completion);
        }
    }
}
