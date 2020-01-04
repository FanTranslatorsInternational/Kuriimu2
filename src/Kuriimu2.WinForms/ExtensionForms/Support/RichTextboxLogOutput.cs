using System;
using System.Windows.Forms;
using Kontract;
using Kore.Logging;

namespace Kuriimu2.WinForms.ExtensionForms.Support
{
    class RichTextboxLogOutput : ILogOutput
    {
        private readonly RichTextBox _richTextBox;

        public RichTextboxLogOutput(RichTextBox richTextBox)
        {
            ContractAssertions.IsNotNull(richTextBox, nameof(richTextBox));

            _richTextBox = richTextBox;
        }

        public void LogLine(LogLevel level, string message)
        {
            _richTextBox.AppendText($"[{level}] {message}{Environment.NewLine}");
        }

        public void Clear()
        {
            _richTextBox.Clear();
        }
    }
}
