using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly int _limit;

        private readonly IList<int> _loggedPositions;
        private readonly IList<int> _loggedLengths;

        public RichTextAreaSink(RichTextArea richTextArea, int limit = 50)
        {
            _richTextArea = richTextArea;
            _limit = limit;

            _loggedPositions = new List<int>();
            _loggedLengths = new List<int>();
        }

        public void Emit(LogEvent logEvent)
        {
            var selectedColor = SelectColor(logEvent.Level);
            var createdLogMessage = logEvent.RenderMessage();

            Application.Instance.Invoke(() => TryLogInternal(selectedColor, createdLogMessage));
        }

        private Color SelectColor(LogEventLevel logLevel)
        {
            switch (logLevel)
            {
                case LogEventLevel.Information:
                    return Themer.Instance.GetTheme().LogInfoColor;

                case LogEventLevel.Warning:
                    return Themer.Instance.GetTheme().LogWarningColor;

                case LogEventLevel.Error:
                    return Themer.Instance.GetTheme().LogErrorColor;

                case LogEventLevel.Fatal:
                    return Themer.Instance.GetTheme().LogFatalColor;

                default:
                    return Themer.Instance.GetTheme().LogDefaultColor;
            }
        }

        private void TryLogInternal(Color logColor, string message)
        {
            try
            {
                LogInternal(logColor, message);
            }
            catch
            {
                // Ignore when writing logging message threw exception
            }
        }

        private void LogInternal(Color logColor, string message)
        {
            var text = message + '\n';

            // Cap log if necessary
            if (_loggedPositions.Count > _limit)
            {
                _richTextArea.Buffer.Delete(new Range<int>(_loggedPositions[0], _loggedLengths[0] - 1));

                var firstLength = _loggedLengths[0];
                _loggedPositions.RemoveAt(0);
                _loggedLengths.RemoveAt(0);

                for (var i = 0; i < _loggedPositions.Count; i++)
                    _loggedPositions[i] -= firstLength;
            }

            // Update text buffer
            var position = _loggedPositions.Count <= 0 ? 0 : _loggedPositions.Last() + _loggedLengths.Last();
            _loggedPositions.Add(position);

            _richTextArea.Buffer.Insert(position, text);
            _loggedLengths.Add(_richTextArea.Text.Length - position - 1);

            _richTextArea.Buffer.SetForeground(new Range<int>(position, _richTextArea.Text.Length - 1), logColor);
        }
    }
}
