using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Controls
{
    public class Kuriimu2ProgressBar : Drawable
    {
        private string _text = string.Empty;
        private int _value;

        private Color _textColor = KnownColors.Black;
        private Color _progressColor = KnownColors.LimeGreen;

        public int Minimum { get; set; }

        public int Maximum { get; set; } = 100;

        public string Text
        {
            get => _text;
            set
            {
                var update = _text != value;
                _text = value;

                if (update) Invalidate();
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                var update = _value != value;
                _value = value;

                if (update) Invalidate();
            }
        }

        public virtual Color ProgressColor
        {
            get => _progressColor;
            set
            {
                var update = _progressColor != value;
                _progressColor = value;

                if (update) Invalidate();
            }
        }

        public virtual Color TextColor
        {
            get => _textColor;
            set
            {
                var update = _textColor != value;
                _textColor = value;

                if (update) Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var controlRect = new RectangleF(
                e.ClipRectangle.X,
                e.ClipRectangle.Y,
                e.ClipRectangle.Width - 1,
                e.ClipRectangle.Height - 1);
            var progressWidth = (int)(controlRect.Width / (double)Maximum * Value);
            var progressRect = new RectangleF(
                e.ClipRectangle.X,
                e.ClipRectangle.Y,
                progressWidth,
                e.ClipRectangle.Height - 1);

            // Draw background
            e.Graphics.FillRectangle(new SolidBrush(KnownColors.Control), controlRect);
            e.Graphics.FillRectangle(new SolidBrush(ProgressColor), progressRect);

            // Draw border
            e.Graphics.DrawRectangle(new Pen(KnownColors.ControlDark), controlRect);

            // Measure string
            var font = new Font(FontFamilies.Sans, 9);
            var stringSize = e.Graphics.MeasureString(font, Text);

            // Draw string
            var pointText = new PointF((Width - stringSize.Width) / 2, (Height - stringSize.Height) / 2);
            e.Graphics.DrawText(font, new SolidBrush(TextColor), pointText.X, pointText.Y, Text);
        }
    }
}
