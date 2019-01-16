using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms
{
    public class ZoomChangedEventArgs : EventArgs
    {
        public double NewZoomLevel { get; set; }
    }

    public class KuriimuPictureBox : PictureBox
    {
        public virtual Color GridColor1 { get; set; } = Color.White;

        public virtual Color GridColor2 { get; set; } = Color.LightGray;

        public virtual int GridSize { get; set; } = 15;

        private double _zoomLevel = 1.0;
        public virtual double ZoomLevel
        {
            get
            {
                return _zoomLevel;
            }
            set
            {
                if (value < MinZoomLevel || value > MaxZoomLevel)
                    throw new ArgumentOutOfRangeException(nameof(ZoomLevel));

                _zoomLevel = value;
            }
        }

        public virtual double MaxZoomLevel => 60.0;

        public virtual double MinZoomLevel => 0.125;

        public event EventHandler<EventArgs> ZoomChanged;

        protected virtual Image ZoomedImage { get; set; }

        protected override bool DoubleBuffered { get; set; } = true;

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (GridSize <= 0)
            {
                base.OnPaintBackground(pevent);
                return;
            }

            pevent.Graphics.FillRectangles(new SolidBrush(GridColor1), GetGridRectangles(x => x % 2).ToArray());
            pevent.Graphics.FillRectangles(new SolidBrush(GridColor2), GetGridRectangles(x => x % 2 == 0 ? 1 : 0).ToArray());
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            OnPaintBackground(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
            pe.Graphics.DrawImage(ZoomedImage ?? Image, new PointF(0, 0));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta == 0)
                return;

            var newZoomLevel = Math.Min(MaxZoomLevel, Math.Max(MinZoomLevel, (e.Delta < 0) ? ZoomLevel / 2 : ZoomLevel * 2));
            if (newZoomLevel == ZoomLevel)
                return;

            ZoomChanged?.Invoke(this, new ZoomChangedEventArgs { NewZoomLevel = newZoomLevel });
            ZoomLevel = newZoomLevel;

            ZoomedImage?.Dispose();
            ZoomedImage = ResizeImage(Image, (int)(Image.Width * ZoomLevel), (int)(Image.Height * ZoomLevel));
            OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));

            base.OnMouseWheel(e);
        }

        private IEnumerable<RectangleF> GetGridRectangles(Func<int, int> getWidthGridStart)
        {
            var gridsPerWidth = Math.Ceiling(Width / (double)GridSize);
            var gridsPerHeight = Math.Ceiling(Height / (double)GridSize);

            for (int h = 0; h < gridsPerHeight; h++)
                for (int w = getWidthGridStart(h); w < gridsPerWidth; w += 2)
                    yield return new RectangleF(w * GridSize, h * GridSize, GridSize, GridSize);
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
