using System;
using System.Windows.Forms;
using Kontract;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;

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

        public void Log(ApplicationLevel applicationLevel, LogLevel level, string message)
        {
            _richTextBox.AppendText($"[{applicationLevel}][{level}] {message}{Environment.NewLine}");
        }
    }
}
