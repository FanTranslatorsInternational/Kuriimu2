using System.Drawing;
using System.Windows.Forms;

namespace Kuriimu2.WinForms.Controls
{
    public class Kuriimu2ProgressBar : Control
    {
        private string _text = string.Empty;
        private int _value;
        private Color _progressColor = Color.LimeGreen;
        private Color _textColor = Color.Black;

        public int Minimum { get; set; }

        public int Maximum { get; set; } = 100;

        public override string Text
        {
            get => _text;
            set
            {
                var update = _text != value;
                _text = value;

                if (update)
                    OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                var update = _value != value;
                _value = value;

                if (update)
                    OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
            }
        }

        public virtual Color ProgressColor
        {
            get => _progressColor;
            set
            {
                var update = _progressColor != value;
                _progressColor = value;

                if (update)
                    OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
            }
        }

        public virtual Color TextColor
        {
            get => _textColor;
            set
            {
                var update = _textColor != value;
                _textColor = value;

                if (update)
                    OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var controlRect = new Rectangle(
                e.ClipRectangle.X,
                e.ClipRectangle.Y,
                e.ClipRectangle.Width - 1,
                e.ClipRectangle.Height - 1);
            var progressWidth = (int)(controlRect.Width / (double)Maximum * Value);
            var progressRect = new Rectangle(
                e.ClipRectangle.X,
                e.ClipRectangle.Y,
                progressWidth,
                e.ClipRectangle.Height - 1);

            // Draw background
            e.Graphics.FillRectangle(new SolidBrush(SystemColors.Control), controlRect);
            e.Graphics.FillRectangle(new SolidBrush(ProgressColor), progressRect);

            // Draw border
            e.Graphics.DrawRectangle(new Pen(SystemColors.ControlDark), controlRect);

            // Draw string
            var stringSize = e.Graphics.MeasureString(Text, DefaultFont);
            var pointText = new PointF((Width - stringSize.Width) / 2, (Height - stringSize.Height) / 2);
            e.Graphics.DrawString(Text, DefaultFont, new SolidBrush(TextColor), pointText);
        }
    }
}
