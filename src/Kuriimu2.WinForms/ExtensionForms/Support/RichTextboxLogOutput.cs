using System;
using System.Windows.Forms;
using Kontract;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;

namespace Kuriimu2.WinForms.ExtensionForms.Support
{
    class RichTextboxLogOutput : ILogOutput
    {
        private delegate void SafeCallDelegate(ApplicationLevel applicationLevel, LogLevel level, string message);

        private readonly RichTextBox _richTextBox;

        public RichTextboxLogOutput(RichTextBox richTextBox)
        {
            ContractAssertions.IsNotNull(richTextBox, nameof(richTextBox));

            _richTextBox = richTextBox;
        }

        public void Log(ApplicationLevel applicationLevel, LogLevel level, string message)
        {
            if (_richTextBox.InvokeRequired)
                _richTextBox.Invoke(new SafeCallDelegate(LogInternal), applicationLevel, level, message);
            else
                LogInternal(applicationLevel, level, message);
        }

        private void LogInternal(ApplicationLevel applicationLevel, LogLevel level, string message)
        {
            _richTextBox.AppendText($"[{applicationLevel}][{level}] {message}{Environment.NewLine}");
        }
    }
}
