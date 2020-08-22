using System;
using System.Drawing;
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
            Color color;
            switch (level)
            {
                case LogLevel.Information:
                    color = Color.FromArgb(0x20, 0xC2, 0x0E);
                    break;

                case LogLevel.Warning:
                    color = Color.Orange;
                    break;

                case LogLevel.Error:
                    color = Color.Red;
                    break;

                case LogLevel.Fatal:
                    color = Color.DarkRed;
                    break;

                default:
                    color = Color.Wheat;
                    break;
            }

            _richTextBox.SelectionColor = color;
            _richTextBox.AppendText($"[{applicationLevel}][{level}] {message}{Environment.NewLine}");
        }
    }
}
