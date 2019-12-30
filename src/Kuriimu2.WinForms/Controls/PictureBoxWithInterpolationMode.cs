using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kuriimu2.WinForms.Controls
{
    /// <summary>
    /// Inherits from PictureBox; adds Interpolation Mode Setting
    /// </summary>
    public class PictureBoxWithInterpolationMode : PictureBox
    {
        [Browsable(true)]
        [Category("Behaviour")]
        public InterpolationMode InterpolationMode { get; set; }

        [Browsable(true)]
        [Category("Behaviour")]
        public PixelOffsetMode PixelOffsetMode { get; set; }

        protected override void OnPaint(PaintEventArgs paintEventArgs)
        {
            paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
            paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode;
            base.OnPaint(paintEventArgs);
        }
    }
}
