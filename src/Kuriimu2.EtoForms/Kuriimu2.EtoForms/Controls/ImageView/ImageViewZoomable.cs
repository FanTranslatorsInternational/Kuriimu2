using System;
using Eto.Drawing;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Controls.ImageView
{
    public class ImageViewZoomable : Drawable
    {
        private readonly IMatrix _transform = Matrix.Create();
        private static readonly SizeF One = SizeF.Empty + 1;

        private PointF _mouseDownPosition;

        public Image Image { get; set; }

        public MouseButtons PanButton { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Image == null)
                return;

            // Calculate starting position
            var posX = (e.ClipRectangle.Width - Image.Size.Width) / 2;
            var posY = (e.ClipRectangle.Height - Image.Size.Height) / 2;
            var position = new PointF(posX, posY);

            e.Graphics.MultiplyTransform(_transform);
            e.Graphics.ImageInterpolation = ImageInterpolation.None;
            e.Graphics.DrawImage(Image, new RectangleF(position, Image.Size));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var scaleSize = One + e.Delta.Height / 6;
            var scale = Matrix.FromScaleAt(scaleSize, e.Location);

            Matrix.Append(_transform, scale);

            Invalidate();

            e.Handled = true;
        }

        public void MoveGraphic(Point offset)
        {
            var move = Matrix.FromTranslation(offset);

            Matrix.Prepend(_transform, move);
        }

        #region Mouse Panning

        protected override void OnMouseDown(MouseEventArgs e)
        {
            e.Handled = e.Buttons == PanButton && e.Modifiers == Keys.None;
            if (!e.Handled)
            {
                base.OnMouseDown(e);
                return;
            }

            _mouseDownPosition = e.Location;
            Cursor = Cursors.Move;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            e.Handled = _mouseDownPosition != PointF.Empty;
            if (!e.Handled)
            {
                base.OnMouseMove(e);
                return;
            }

            var move = Matrix.FromTranslation(e.Location - _mouseDownPosition);

            Matrix.Append(_transform, move);

            _mouseDownPosition = e.Location;

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            e.Handled = _mouseDownPosition != PointF.Empty;
            if (!e.Handled)
            {
                base.OnMouseUp(e);
                return;
            }

            _mouseDownPosition = PointF.Empty;
            Cursor = Cursors.Default;
        }

        #endregion
    }
}
