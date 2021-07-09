using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Controls
{
    public class ScrollableEx : Drawable
    {
        private const int ScrollUnit_ = 120;

        private ScrollBarInformation _scrollBarInfo;
        private PointF _mouseLocation;
        private bool _scrollVertically;
        private bool _scrollHorizontally;

        public new IScrollableContent Content { get; set; }

        public ScrollableEx(IScrollableContent content)
        {
            Content = content;
        }

        public void ScrollToPosition(PointF position)
        {
            GetScrollBarInformation().ScrollToPosition(position);
        }

        #region Mouse events

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            var verticalRect = GetScrollBarInformation().GetSliderRect(ScrollBarOrientation.Vertical);
            var horizontalRect = GetScrollBarInformation().GetSliderRect(ScrollBarOrientation.Horizontal);

            _scrollVertically = verticalRect.Contains(e.Location);
            _scrollHorizontally = horizontalRect.Contains(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _scrollVertically = false;
            _scrollHorizontally = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_mouseLocation == e.Location)
                return;

            var shouldScrollVertically = _scrollVertically && e.Location.Y != _mouseLocation.Y;
            if (shouldScrollVertically)
                GetScrollBarInformation().MoveSliderBy(ScrollBarOrientation.Vertical, e.Location.Y - _mouseLocation.Y);

            var shouldScrollHorizontally = _scrollHorizontally && e.Location.X != _mouseLocation.X;
            if (shouldScrollHorizontally)
                GetScrollBarInformation().MoveSliderBy(ScrollBarOrientation.Horizontal, e.Location.X - _mouseLocation.X);

            if (shouldScrollHorizontally || shouldScrollVertically)
                Invalidate();

            _mouseLocation = e.Location;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var scrollBarInfo = GetScrollBarInformation();

            var newScrollPosition = new PointF(
                scrollBarInfo.ScrollPosition.X + -e.Delta.Width * ScrollUnit_,
                scrollBarInfo.ScrollPosition.Y + -e.Delta.Height * ScrollUnit_);
            ScrollToPosition(newScrollPosition);

            Invalidate();
        }

        #endregion

        protected override void OnSizeChanged(EventArgs e)
        {
            GetScrollBarInformation().UpdateClientSize(Size);

            Invalidate();
        }

        #region Paint events

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var scrollBarInfo = GetScrollBarInformation();

            var verticalScrollBarWidth = scrollBarInfo.GetScrollBarWidth(ScrollBarOrientation.Vertical);
            var horizontalScrollBarWidth = scrollBarInfo.GetScrollBarWidth(ScrollBarOrientation.Horizontal);

            // Draw content first
            var width = Content.Size.Width <= 0
                ? Size.Width - verticalScrollBarWidth
                : Math.Min(Content.Size.Width, Size.Width - verticalScrollBarWidth);
            var height = Content.Size.Height <= 0
                ? Size.Height - horizontalScrollBarWidth
                : Math.Min(Content.Size.Height, Size.Height - horizontalScrollBarWidth);
            var clipRectangle = new RectangleF(scrollBarInfo.ScrollPosition, new SizeF(width, height));
            Content.Paint(new RelativeGraphics(e.Graphics, scrollBarInfo.ScrollPosition), clipRectangle);

            // Draw scrollbars
            scrollBarInfo.DrawScrollBar(e.Graphics, ScrollBarOrientation.Horizontal);
            scrollBarInfo.DrawScrollBar(e.Graphics, ScrollBarOrientation.Vertical);
        }

        #endregion

        #region Support

        private ScrollBarInformation GetScrollBarInformation()
        {
            if (_scrollBarInfo != null)
                return _scrollBarInfo;

            return _scrollBarInfo = new ScrollBarInformation(Content.Size, Size);
        }

        #endregion
    }

    class ScrollBarInformation
    {
        private const int ScrollBarWidth_ = 16;
        private const int MinSliderLength_ = 20;
        private const int ArrowLength_ = 17;

            private Color ScrollBarBackgroundColor => Color.FromArgb(0xf0, 0xf0, 0xf0);
        //private Color ScrollBarBackgroundColor => Color.FromArgb(0, 0, 0);
        private Color ScrollBarColor => Color.FromArgb(0xcd, 0xcd, 0xcd);
        private Color ScrollBarDarkColor => Color.FromArgb(0xa6, 0xa6, 0xa6);
        private Color ScrollBarArrowBackgroundColor => ScrollBarBackgroundColor;
        private Color ScrollBarArrowBackgroundDarkColor => Color.FromArgb(0xda, 0xda, 0xda);
        private Color ScrollBarArrow => Color.FromArgb(0x60, 0x60, 0x60);

        private Size _contentSize;
        private Size _clientSizeWithoutBar;
        private Size _clientSize;

        public PointF ScrollPosition { get; private set; }

        private bool _isHorizontalVisible;
        private int _horizontalSliderSize;

        private bool _isVerticalVisible;
        private int _verticalSliderSize;

        public ScrollBarInformation(Size contentSize, Size clientSize)
        {
            UpdateInformation(contentSize, clientSize);
        }

        public void UpdateClientSize(Size clientSize)
        {
            UpdateInformation(_contentSize, clientSize);
        }

        public void ScrollToPosition(PointF position)
        {
            var newX = Math.Max(0, position.X);
            var newY = Math.Max(0, position.Y);

            if (_contentSize.Height - _clientSize.Height > 0)
                newY = Math.Min(newY, _contentSize.Height - _clientSizeWithoutBar.Height);
            if (_contentSize.Width - _clientSize.Width > 0)
                newX = Math.Min(newX, _contentSize.Width - _clientSizeWithoutBar.Width);

            ScrollPosition = new PointF(newX, newY);
        }

        public void MoveSliderBy(ScrollBarOrientation orientation, float value)
        {
            SetSliderPosition(orientation, GetSliderPosition(orientation) + value);
        }

        public void DrawScrollBar(Graphics g, ScrollBarOrientation orientation)
        {
            switch (orientation)
            {
                case ScrollBarOrientation.Horizontal:
                    if (!_isHorizontalVisible) break;

                    // Background
                    var backgroundRect = new RectangleF(new PointF(0, _clientSize.Height - ScrollBarWidth_),
                        new SizeF(_clientSize.Width - GetScrollBarWidth(ScrollBarOrientation.Vertical), ScrollBarWidth_));
                    g.FillRectangle(ScrollBarBackgroundColor, backgroundRect);

                    // Arrows
                    g.DrawLines(ScrollBarArrow,
                        new PointF(11, _clientSize.Height - ScrollBarWidth_ + ScrollBarWidth_ / 4),
                        new PointF(6, _clientSize.Height - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 2),
                        new PointF(11, _clientSize.Height - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 3));
                    g.DrawLines(ScrollBarArrow,
                        new PointF(_clientSize.Width - GetScrollBarWidth(ScrollBarOrientation.Vertical) - ArrowLength_ + 6, _clientSize.Height - ScrollBarWidth_ + ScrollBarWidth_ / 4),
                        new PointF(_clientSize.Width - GetScrollBarWidth(ScrollBarOrientation.Vertical) - ArrowLength_ + 11, _clientSize.Height - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 2),
                        new PointF(_clientSize.Width - GetScrollBarWidth(ScrollBarOrientation.Vertical) - ArrowLength_ + 6, _clientSize.Height - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 3));

                    // Slider
                    g.FillRectangle(ScrollBarColor, GetSliderRect(ScrollBarOrientation.Horizontal));

                    break;

                case ScrollBarOrientation.Vertical:
                    if (!_isVerticalVisible) break;

                    // Background
                    var backgroundRect1 = new RectangleF(new PointF(_clientSize.Width - ScrollBarWidth_, 0),
                        new SizeF(ScrollBarWidth_, _clientSize.Height - GetScrollBarWidth(ScrollBarOrientation.Horizontal)));
                    g.FillRectangle(ScrollBarBackgroundColor, backgroundRect1);

                    // Arrows
                    g.DrawLines(ScrollBarArrow,
                        new PointF(_clientSize.Width - ScrollBarWidth_ + ScrollBarWidth_ / 4, 11),
                        new PointF(_clientSize.Width - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 2, 6),
                        new PointF(_clientSize.Width - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 3, 11));
                    g.DrawLines(ScrollBarArrow,
                        new PointF(_clientSize.Width - ScrollBarWidth_ + ScrollBarWidth_ / 4, _clientSize.Height - GetScrollBarWidth(ScrollBarOrientation.Horizontal) - ArrowLength_ + 6),
                        new PointF(_clientSize.Width - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 2, _clientSize.Height - GetScrollBarWidth(ScrollBarOrientation.Horizontal) - ArrowLength_ + 11),
                        new PointF(_clientSize.Width - ScrollBarWidth_ + ScrollBarWidth_ / 4 * 3, _clientSize.Height - GetScrollBarWidth(ScrollBarOrientation.Horizontal) - ArrowLength_ + 6));

                    // Slider
                    g.FillRectangle(ScrollBarColor, GetSliderRect(ScrollBarOrientation.Vertical));

                    break;
            }
        }

        public int GetScrollBarWidth(ScrollBarOrientation orientation)
        {
            switch (orientation)
            {
                case ScrollBarOrientation.Horizontal:
                    return _isHorizontalVisible ? ScrollBarWidth_ : 0;

                case ScrollBarOrientation.Vertical:
                    return _isVerticalVisible ? ScrollBarWidth_ : 0;

                default:
                    throw new InvalidOperationException($"Unsupported orientation: {orientation}.");
            }
        }

        public RectangleF GetSliderRect(ScrollBarOrientation orientation)
        {
            var sliderPosition = GetSliderPosition(orientation);
            switch (orientation)
            {
                case ScrollBarOrientation.Horizontal:
                    if (!_isHorizontalVisible) return default;
                    return new RectangleF(new PointF(sliderPosition, _clientSizeWithoutBar.Height), new SizeF(_horizontalSliderSize, ScrollBarWidth_ - 1));

                case ScrollBarOrientation.Vertical:
                    if (!_isVerticalVisible) return default;
                    return new RectangleF(new PointF(_clientSizeWithoutBar.Width, sliderPosition), new SizeF(ScrollBarWidth_ - 1, _verticalSliderSize));

                default:
                    throw new InvalidOperationException($"Unsupported orientation: {orientation}.");
            }
        }

        private float GetSliderPosition(ScrollBarOrientation orientation)
        {
            var sliderMin = ArrowLength_;
            int sliderMax;

            switch (orientation)
            {
                case ScrollBarOrientation.Horizontal:
                    sliderMax = _clientSizeWithoutBar.Width - ArrowLength_ - _horizontalSliderSize;
                    return MapRange(ScrollPosition.X, _clientSizeWithoutBar.Width, _contentSize.Width, sliderMin, sliderMax) + sliderMin;

                case ScrollBarOrientation.Vertical:
                    sliderMax = _clientSizeWithoutBar.Height - ArrowLength_ - _verticalSliderSize;
                    return MapRange(ScrollPosition.Y, _clientSizeWithoutBar.Height, _contentSize.Height, sliderMin, sliderMax) + sliderMin;

                default:
                    throw new InvalidOperationException($"Unsupported orientation: {orientation}.");
            }
        }

        private void SetSliderPosition(ScrollBarOrientation orientation, float value)
        {
            var sliderMin = ArrowLength_;
            int sliderMax;

            switch (orientation)
            {
                case ScrollBarOrientation.Horizontal:
                    sliderMax = _clientSizeWithoutBar.Width - ArrowLength_ - _horizontalSliderSize;
                    var newX = MapRange(value - sliderMin, sliderMin, sliderMax, _clientSizeWithoutBar.Width, _contentSize.Width);

                    newX = Math.Min(Math.Max(0, newX), _contentSize.Width - _clientSizeWithoutBar.Width);

                    ScrollPosition = new PointF(newX, ScrollPosition.Y);
                    break;

                case ScrollBarOrientation.Vertical:
                    sliderMax = _clientSizeWithoutBar.Height - ArrowLength_ - _verticalSliderSize;
                    var newY = MapRange(value - sliderMin, sliderMin, sliderMax, _clientSizeWithoutBar.Height, _contentSize.Height);

                    newY = Math.Min(Math.Max(0, newY), _contentSize.Height - _clientSizeWithoutBar.Height);

                    ScrollPosition = new PointF(ScrollPosition.X, newY);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported orientation: {orientation}.");
            }
        }

        private void UpdateInformation(Size contentSize, Size clientSize)
        {
            // Visibility
            _isHorizontalVisible = contentSize.Width > clientSize.Width;
            _isVerticalVisible = contentSize.Height > clientSize.Height;

            // Set internal sizes
            _contentSize = contentSize;
            _clientSize = clientSize;

            var verticalBarWidth = GetScrollBarWidth(ScrollBarOrientation.Vertical);
            var horizontalBarWidth = GetScrollBarWidth(ScrollBarOrientation.Horizontal);
            _clientSizeWithoutBar = new Size(
                clientSize.Width - verticalBarWidth,
                clientSize.Height - horizontalBarWidth);

            // Slider sizes
            var contentWidth = Math.Max(contentSize.Width, _clientSizeWithoutBar.Width);
            _horizontalSliderSize = (int)MapRange(_clientSizeWithoutBar.Width, contentWidth, _clientSizeWithoutBar.Width - ArrowLength_ * 2);
            _horizontalSliderSize = Math.Max(MinSliderLength_, _horizontalSliderSize);

            var contentHeight = Math.Max(contentSize.Height, _clientSizeWithoutBar.Height);
            _verticalSliderSize = (int)MapRange(_clientSizeWithoutBar.Height, contentHeight, _clientSizeWithoutBar.Height - ArrowLength_ * 2);
            _verticalSliderSize = Math.Max(MinSliderLength_, _verticalSliderSize);

            // Scroll position
            var scrollPosition = ScrollPosition;
            if (!_isHorizontalVisible) scrollPosition = new PointF(0, scrollPosition.Y);
            if (!_isVerticalVisible) scrollPosition = new PointF(scrollPosition.X, 0);
            ScrollToPosition(scrollPosition);
        }

        private float MapRange(float value, int inputRangeStart, int inputRangeEnd, int outputRangeStart, int outputRangeEnd)
        {
            return MapRange(value, inputRangeEnd - inputRangeStart, outputRangeEnd - outputRangeStart);
        }

        private float MapRange(float value, int inputRange, int outputRange)
        {
            return value / inputRange * outputRange;
        }
    }

    public enum ScrollBarOrientation
    {
        Horizontal,
        Vertical
    }

    public interface IScrollableContent
    {
        Size Size { get; set; }

        void Paint(RelativeGraphics g, RectangleF clipRectangle);
    }

    public class RelativeGraphics
    {
        private readonly Graphics _g;
        private readonly PointF _rp;

        public RelativeGraphics(Graphics g, PointF rp)
        {
            _g = g;
            _rp = rp;
        }

        public void FillRectangle(Color c, RectangleF r) => _g.FillRectangle(c, TransformRect(r));
        public void FillRectangle(Color c, float x, float y, float w, float h) => _g.FillRectangle(c, TransformX(x), TransformY(y), w, h);
        public void FillRectangle(Brush b, RectangleF r) => _g.FillRectangle(b, TransformRect(r));
        public void FillRectangle(Brush b, float x, float y, float w, float h) => _g.FillRectangle(b, TransformX(x), TransformY(y), w, h);

        public void DrawImage(Image img, PointF p) => _g.DrawImage(img, TransformPoint(p));
        public void DrawImage(Image img, RectangleF r) => _g.DrawImage(img, TransformRect(r));
        public void DrawImage(Image img, RectangleF s, PointF d) => _g.DrawImage(img, s, TransformPoint(d));
        public void DrawImage(Image img, RectangleF s, RectangleF d) => _g.DrawImage(img, s, TransformRect(d));
        public void DrawImage(Image img, float x, float y) => _g.DrawImage(img, TransformX(x), TransformY(y));

        public void DrawLines(Color c, IEnumerable<PointF> p) => _g.DrawLines(c, p.Select(TransformPoint));
        public void DrawLines(Color c, params PointF[] p) => _g.DrawLines(c, p.Select(TransformPoint).ToArray());
        public void DrawLines(Pen pn, IEnumerable<PointF> p) => _g.DrawLines(pn, p.Select(TransformPoint));
        public void DrawLines(Pen pn, params PointF[] p) => _g.DrawLines(pn, p.Select(TransformPoint).ToArray());

        public void DrawArc(Color c, RectangleF r, float sta, float swa) => _g.DrawArc(c, TransformRect(r), sta, swa);
        public void DrawArc(Color c, float x, float y, float w, float h, float sta, float swa) => _g.DrawArc(c, TransformX(x), TransformY(y), w, h, sta, swa);
        public void DrawArc(Pen pn, RectangleF r, float sta, float swa) => _g.DrawArc(pn, TransformRect(r), sta, swa);
        public void DrawArc(Pen pn, float x, float y, float w, float h, float sta, float swa) => _g.DrawArc(pn, TransformX(x), TransformY(y), w, h, sta, swa);

        public void DrawEllipse(Color c, RectangleF r) => _g.DrawEllipse(c, TransformRect(r));
        public void DrawEllipse(Color c, float x, float y, float w, float h) => _g.DrawEllipse(c, TransformX(x), TransformY(y), w, h);
        public void DrawEllipse(Pen pn, RectangleF r) => _g.DrawEllipse(pn, TransformRect(r));
        public void DrawEllipse(Pen pn, float x, float y, float w, float h) => _g.DrawEllipse(pn, TransformX(x), TransformY(y), w, h);

        public void DrawInsetRectangle(Color tlc, Color brc, RectangleF r, int w = 1) => _g.DrawInsetRectangle(tlc, brc, TransformRect(r), w);

        public void DrawLine(Color c, PointF s, PointF e) => _g.DrawLine(c, TransformPoint(s), TransformPoint(e));
        public void DrawLine(Color c, float x, float y, float x1, float y1) => _g.DrawLine(c, TransformX(x), TransformY(y), TransformX(x1), TransformY(y1));
        public void DrawLine(Pen pn, PointF s, PointF e) => _g.DrawLine(pn, TransformPoint(s), TransformPoint(e));
        public void DrawLine(Pen pn, float x, float y, float x1, float y1) => _g.DrawLine(pn, TransformX(x), TransformY(y), TransformX(x1), TransformY(y1));

        public void DrawText(Font f, Brush b, PointF p, string t) => _g.DrawText(f, b, TransformPoint(p), t);
        public void DrawText(Font f, Brush b, RectangleF r, string t, FormattedTextWrapMode w = FormattedTextWrapMode.Word,
            FormattedTextAlignment a = FormattedTextAlignment.Left, FormattedTextTrimming tr = FormattedTextTrimming.WordEllipsis) => _g.DrawText(f, b, TransformRect(r), t, w, a, tr);
        public void DrawText(Font f, Brush b, float x, float y, string t) => _g.DrawText(f, b, TransformX(x), TransformY(y), t);
        public void DrawText(Font f, Color c, PointF p, string t) => _g.DrawText(f, c, TransformPoint(p), t);
        public void DrawText(Font f, Color c, float x, float y, string t) => _g.DrawText(f, c, TransformX(x), TransformY(y), t);

        public void DrawPolygon(Color c, params PointF[] p) => _g.DrawPolygon(c, p.Select(TransformPoint).ToArray());
        public void DrawPolygon(Pen pn, params PointF[] p) => _g.DrawPolygon(pn, p.Select(TransformPoint).ToArray());

        public void DrawRectangle(Color c, RectangleF r) => _g.DrawRectangle(c, TransformRect(r));
        public void DrawRectangle(Color c, float x, float y, float w, float h) => _g.DrawRectangle(c, TransformX(x), TransformY(y), w, h);
        public void DrawRectangle(Pen pn, RectangleF r) => _g.DrawRectangle(pn, TransformRect(r));
        public void DrawRectangle(Pen pn, float x, float y, float w, float h) => _g.DrawRectangle(pn, TransformX(x), TransformY(y), w, h);

        private RectangleF TransformRect(RectangleF r) => new RectangleF(TransformX(r.X), TransformY(r.Y), r.Width, r.Height);
        private PointF TransformPoint(PointF p) => new PointF(TransformX(p.X), TransformY(p.Y));
        private float TransformX(float x) => x - _rp.X;
        private float TransformY(float y) => y - _rp.Y;
    }
}
