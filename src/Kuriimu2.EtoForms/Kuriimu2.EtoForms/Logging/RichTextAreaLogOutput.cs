using System;
using Eto.Drawing;
using Eto.Forms;
using Kontract.Interfaces.Logging;
using Kontract.Models.Logging;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Logging
{
    class RichTextAreaLogOutput:ILogOutput
    {
        private readonly RichTextArea _richTextArea;

        public RichTextAreaLogOutput(RichTextArea richTextArea)
        {
            _richTextArea = richTextArea;
        }

        public void Log(ApplicationLevel applicationLevel, LogLevel level, string message)
        {
            var selectedColor = SelectColor(level);
            var createdLogMessage = CreateLogMessage(applicationLevel, level, message);

            Application.Instance.Invoke(() => LogInternal(selectedColor, createdLogMessage));
        }

        private Color SelectColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    return KnownColors.NeonGreen;

                case LogLevel.Warning:
                    return KnownColors.Orange;

                case LogLevel.Error:
                    return KnownColors.Red;

                case LogLevel.Fatal:
                    return KnownColors.DarkRed;

                default:
                    return KnownColors.Wheat;
            }
        }

        private string CreateLogMessage(ApplicationLevel applicationLevel, LogLevel logLevel, string message)
        {
            return $"[{applicationLevel}][{logLevel}] {message}{Environment.NewLine}";
        }

        private void LogInternal(Color logColor, string message)
        {
            _richTextArea.TextColor = logColor;
            _richTextArea.Append(message);
        }
    }
}
