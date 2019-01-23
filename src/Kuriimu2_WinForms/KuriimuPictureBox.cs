using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Kuriimu2_WinForms
{
    public class ZoomChangedEventArgs : EventArgs
    {
        public ZoomChangedEventArgs(double newZoomLevel)
        {
            NewZoomLevel = newZoomLevel;
        }

        public double NewZoomLevel { get; set; }
    }

    public class KuriimuPictureBox : PictureBox
    {
        public virtual Color GridColor1 { get; set; } = Color.White;

        public virtual Color GridColor2 { get; set; } = Color.LightGray;

        public virtual int GridSize { get; set; } = 15;

        private float _zoomLevel = 1.0f;
        public virtual float ZoomLevel
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
                ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(value));
            }
        }

        protected virtual PointF ImagePosition { get; set; } = new PointF(0, 0);

        public virtual double MaxZoomLevel => 64.0;

        public virtual double MinZoomLevel => 0.125;

        private Image _image;
        public new Image Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
                ZoomLevel = 1.0f;
                ZoomedImage?.Dispose();
                ImagePosition = new PointF(0, 0);
            }
        }

        public event EventHandler<ZoomChangedEventArgs> ZoomChanged;

        protected virtual Image ZoomedImage { get; set; }

        protected override bool DoubleBuffered { get; set; } = true;

        private PointF GetTopLeftImageCorner(Image img) => new PointF(Width / 2 - img.Width / 2 + ImagePosition.X * ZoomLevel, Height / 2 - img.Height / 2 + ImagePosition.Y * ZoomLevel);

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
            var img = ZoomedImage ?? Image;
            pe.Graphics.DrawImage(img, GetTopLeftImageCorner(img));
        }

        private bool _mouseDown = false;
        private Point _previousMouseLocation = new Point(0, 0);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _previousMouseLocation = e.Location;
            if (!_mouseDown)
                _mouseDown = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _previousMouseLocation = e.Location;
            if (_mouseDown)
                _mouseDown = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_mouseDown && !_previousMouseLocation.Equals(e.Location) && LocationInImage(e.Location))
            {
                var deltaLocation = new Point(e.X - _previousMouseLocation.X, e.Y - _previousMouseLocation.Y);
                ImagePosition = new PointF(ImagePosition.X + deltaLocation.X / ZoomLevel, ImagePosition.Y + deltaLocation.Y / ZoomLevel);

                OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));

                _previousMouseLocation = e.Location;
            }
        }

        private bool LocationInImage(Point location)
        {
            var img = ZoomedImage ?? Image;
            var topLeftPoint = GetTopLeftImageCorner(img);
            var imageRect = new RectangleF(topLeftPoint, new SizeF(img.Width, img.Height));
            return imageRect.Contains(location);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta == 0)
                return;

            float newZoomLevel = (float)Math.Min(MaxZoomLevel, Math.Max(MinZoomLevel, (e.Delta < 0) ? ZoomLevel / 2 : ZoomLevel * 2));
            if (newZoomLevel == ZoomLevel) return;
            ZoomLevel = newZoomLevel;

            ZoomImage();

            // Calculate mouse dependant zoomed position
            //var topLeft = GetTopLeftImageCorner(ZoomedImage);
            //var deltaPosToMouse = new PointF(topLeft.X - e.X, topLeft.Y - e.Y);
            //ImagePosition = new PointF(ImagePosition.X + ((e.Delta < 0) ? deltaPosToMouse.X / 2 : deltaPosToMouse.X * 2), ImagePosition.Y + ((e.Delta < 0) ? deltaPosToMouse.Y / 2 : deltaPosToMouse.Y * 2));

            OnPaint(new PaintEventArgs(CreateGraphics(), DisplayRectangle));
        }

        private void ZoomImage()
        {
            ZoomedImage?.Dispose();
            ZoomedImage = ResizeImage(Image, (int)(Image.Width * ZoomLevel), (int)(Image.Height * ZoomLevel));
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
