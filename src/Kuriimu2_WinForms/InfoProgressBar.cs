using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms
{
    class InfoProgressBar : ProgressBar
    {
        private string _text;
        private Color _textColor;
        private Color _progColor;

        /// <summary>
        /// Gets or sets the text shown on the progressbar
        /// </summary>
        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
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
                OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
            }
        }

        /// <summary>
        /// Gets or sets the color of the progressbar
        /// </summary>
        public Color ProgressColor
        {
            get => _progColor;
            set
            {
                _progColor = value;
                OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
            }
        }

        public InfoProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            _textColor = Color.Black;
            _progColor = Color.ForestGreen;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var controlRect = new Rectangle(DisplayRectangle.X, DisplayRectangle.Y, DisplayRectangle.Width - 1, DisplayRectangle.Height - 1);
            var progressRect = new Rectangle(DisplayRectangle.X, DisplayRectangle.Y, Convert.ToInt32((double)DisplayRectangle.Width / Maximum * Value - 1), DisplayRectangle.Height - 1);

            // Draw background
            e.Graphics.FillRectangle(new SolidBrush(SystemColors.Control), controlRect);
            e.Graphics.FillRectangle(new SolidBrush(_progColor), progressRect);

            // Draw border
            e.Graphics.DrawRectangle(new Pen(SystemColors.ControlDark), controlRect);

            // Draw string
            var stringSize = e.Graphics.MeasureString(Text, DefaultFont);
            var pointText = new PointF((Width - stringSize.Width) / 2, (Height - stringSize.Height) / 2);
            e.Graphics.DrawString(Text, DefaultFont, new SolidBrush(_textColor), pointText);
        }
    }
}
