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
        private delegate void SafeCallDelegate(Color logColor, string message);

        private readonly RichTextBox _richTextBox;

        public RichTextboxLogOutput(RichTextBox richTextBox)
        {
            ContractAssertions.IsNotNull(richTextBox, nameof(richTextBox));

            _richTextBox = richTextBox;
        }

        public void Log(ApplicationLevel applicationLevel, LogLevel level, string message)
        {
            var selectedColor = SelectColor(level);
            var createdLogMessage = CreateLogMessage(applicationLevel, level, message);

            if (_richTextBox.InvokeRequired)
                _richTextBox.Invoke(new SafeCallDelegate(LogInternal), selectedColor, createdLogMessage);
            else
                LogInternal(selectedColor, createdLogMessage);
        }

        private Color SelectColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    return Color.FromArgb(0x20, 0xC2, 0x0E);

                case LogLevel.Warning:
                    return Color.Orange;

                case LogLevel.Error:
                    return Color.Red;

                case LogLevel.Fatal:
                    return Color.DarkRed;

                default:
                    return Color.Wheat;
            }
        }

        private string CreateLogMessage(ApplicationLevel applicationLevel, LogLevel logLevel, string message)
        {
            return $"[{applicationLevel}][{logLevel}] {message}{Environment.NewLine}";
        }

        private void LogInternal(Color logColor, string message)
        {
            _richTextBox.SelectionColor = logColor;
            _richTextBox.AppendText(message);
        }
    }
}
