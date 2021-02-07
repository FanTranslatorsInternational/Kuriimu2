using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;
using Serilog.Core;
using Serilog.Events;

namespace Kuriimu2.EtoForms.Logging
{
    class RichTextAreaSink : ILogEventSink
    {
        private readonly RichTextArea _richTextArea;

        public RichTextAreaSink(RichTextArea richTextArea)
        {
            _richTextArea = richTextArea;
        }

        public void Emit(LogEvent logEvent)
        {
            var selectedColor = SelectColor(logEvent.Level);
            var createdLogMessage = logEvent.RenderMessage();

            Application.Instance.Invoke(() => LogInternal(selectedColor, createdLogMessage));
        }

        private Color SelectColor(LogEventLevel logLevel)
        {
            switch (logLevel)
            {
                case LogEventLevel.Information:
                    return KnownColors.NeonGreen;

                case LogEventLevel.Warning:
                    return KnownColors.Orange;

                case LogEventLevel.Error:
                    return KnownColors.Red;

                case LogEventLevel.Fatal:
                    return KnownColors.DarkRed;

                default:
                    return KnownColors.Wheat;
            }
        }

        private void LogInternal(Color logColor, string message)
        {
            _richTextArea.TextColor = logColor;
            _richTextArea.Append(message);
        }
    }
}
