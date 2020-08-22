using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kuriimu2.WinForms.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public class InfoToolStripProgressBar : ToolStripProgressBar
    {
        private string _text = string.Empty;
        private Color _textColor;
        private Color _progressColor;

        /// <summary>
        /// Gets or sets the text shown on the progressbar
        /// </summary>
        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPaint(new PaintEventArgs(ProgressBar.CreateGraphics(), ProgressBar.DisplayRectangle));
            }
        }

        /// <summary>
        /// Gets or sets the text color of the progressbar
        /// </summary>
        public Color TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                OnPaint(new PaintEventArgs(ProgressBar.CreateGraphics(), ProgressBar.DisplayRectangle));
            }
        }

        /// <summary>
        /// Gets or sets the color of the progressbar
        /// </summary>
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                OnPaint(new PaintEventArgs(ProgressBar.CreateGraphics(), ProgressBar.DisplayRectangle));
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public InfoToolStripProgressBar()
        {
            ProgressBar.Style = ProgressBarStyle.Continuous;
            _textColor = Color.Black;
            _progressColor = Color.ForestGreen;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var controlRect = new Rectangle(
                ProgressBar.DisplayRectangle.X,
                ProgressBar.DisplayRectangle.Y,
                ProgressBar.DisplayRectangle.Width - 1,
                ProgressBar.DisplayRectangle.Height - 1);
            var progressRect = new Rectangle(
                ProgressBar.DisplayRectangle.X,
                ProgressBar.DisplayRectangle.Y,
                Convert.ToInt32((double)ProgressBar.DisplayRectangle.Width / Maximum * Value - 1),
                ProgressBar.DisplayRectangle.Height - 1);

            // Draw background
            e.Graphics.FillRectangle(new SolidBrush(SystemColors.Control), controlRect);
            ProgressBarRenderer.DrawHorizontalBar(e.Graphics, progressRect);
            //e.Graphics.FillRectangle(new SolidBrush(_progressColor), progressRect);

            // Draw border
            e.Graphics.DrawRectangle(new Pen(SystemColors.ControlDark), controlRect);

            // Draw string
            var stringSize = e.Graphics.MeasureString(Text, Control.DefaultFont);
            var pointText = new PointF((Width - stringSize.Width) / 2, (Height - stringSize.Height) / 2);
            e.Graphics.DrawString(Text, Control.DefaultFont, new SolidBrush(_textColor), pointText);
        }
    }
}
