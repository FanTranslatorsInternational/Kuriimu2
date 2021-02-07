using Eto.Drawing;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Controls.ImageView
{
    public class DragScrollable : Scrollable
    {
        private PointF _initScrollPos;

        public MouseButtons DragButton { get; set; }

        public new Control Content
        {
            get => base.Content;
            set
            {
                if (base.Content != null)
                {
                    base.Content.MouseDown -= content_MouseDown;
                    base.Content.MouseMove -= content_MouseMove;
                    base.Content.MouseUp -= content_MouseUp;
                }

                base.Content = value;

                if (base.Content != null)
                {
                    base.Content.MouseDown += content_MouseDown;
                    base.Content.MouseMove += content_MouseMove;
                    base.Content.MouseUp += content_MouseUp;
                }
            }
        }

        #region Mouse Scroling

        private void content_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = _initScrollPos != PointF.Empty;
            if (!e.Handled) 
                return;

            var factor = 0.9f; // scroll speed adjustment

            var delta = e.Location - _initScrollPos;
            var move = _initScrollPos + delta * factor;

            ScrollPosition = (Point)(_initScrollPos + move);
        }

        private void content_MouseDown(object sender, MouseEventArgs e)
        {
            e.Handled = e.Buttons == DragButton && e.Modifiers == Keys.None;
            if (!e.Handled)
                return;

            _initScrollPos = ScrollPosition - e.Location;
            Cursor = Cursors.Move;
        }

        private void content_MouseUp(object sender, MouseEventArgs e)
        {
            e.Handled = _initScrollPos != PointF.Empty;
            if (!e.Handled)
                return;

            _initScrollPos = PointF.Empty;
            Cursor = Cursors.Default;
        }

        #endregion
    }
}
