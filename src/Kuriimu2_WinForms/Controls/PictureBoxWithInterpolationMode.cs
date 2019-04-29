using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.Controls
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
            //var rect = new Rectangle(Location, Size);
            //paintEventArgs.Graphics.DrawImage(Image, new PointF[] {
            //        new PointF(rect.Left, rect.Top),
            //        new PointF(rect.Right, rect.Top),
            //        new PointF(rect.Left, rect.Bottom)
            //    },
            //    new Rectangle(0, 0, Image.Width, Image.Height), GraphicsUnit.Pixel);
            //base.OnPaint(paintEventArgs);

            //paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
            //paintEventArgs.Graphics.DrawImage(Image,new Rectangle(Location,Size));
            //base.OnPaint(paintEventArgs);
        }
    }
}
