using System;
using Kore.Progress;
using Kuriimu2.WinForms.Controls;

namespace Kuriimu2.WinForms.Progress
{
    class ToolStripProgressBarOutput : BaseConcurrentProgressOutput
    {
        private readonly Kuriimu2ProgressBarToolStrip _progressBar;

		public ToolStripProgressBarOutput(Kuriimu2ProgressBarToolStrip progressBar, int updateInterval) : 
			base(updateInterval)
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
