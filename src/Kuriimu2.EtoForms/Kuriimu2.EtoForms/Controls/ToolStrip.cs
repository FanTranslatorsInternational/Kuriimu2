using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Controls
{
    public class ToolStrip : Drawable
    {
        private (RectangleF, ToolStripItem) _hoveredItem;
        private SizeF _size;

        public ToolStripItemCollection Items { get; }

        public int Spacing { get; set; }

        public new SizeF Size
        {
            get => _size;
            set
            {
                _size = new SizeF(value.Width, ToolStripItem.Height);
                Invalidate();
            }
        }

        public ToolStrip()
        {
            Items = new ToolStripItemCollection(this);

            BackgroundColor = KnownColors.White;
            Padding = new Padding(3);

            MouseMove += ToolStrip_MouseMove;
            MouseDown += ToolStrip_MouseDown;
            MouseUp += ToolStrip_MouseUp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = new RectangleF(e.ClipRectangle.Location, new SizeF(e.ClipRectangle.Width, ToolStripItem.Height + Padding.Top + Padding.Bottom));

            base.OnPaint(new PaintEventArgs(g, rect));
            OnPaintInternal(g, rect);
        }

        private void OnPaintInternal(Graphics graphics, RectangleF clipRectangle)
        {
            var drawLocation = new PointF(clipRectangle.X + Padding.Left, clipRectangle.Y + Padding.Top);

            foreach (var item in Items)
            {
                var itemWidth = item.Paint(graphics, drawLocation);
                drawLocation = new PointF(drawLocation.X + itemWidth + Spacing, drawLocation.Y);
            }
        }

        private void ToolStrip_MouseDown(object sender, MouseEventArgs e)
        {
            _hoveredItem.Item2?.OnMouseDown(e);
        }

        private void ToolStrip_MouseUp(object sender, MouseEventArgs e)
        {
            _hoveredItem.Item2?.OnMouseUp(e);
        }

        private void ToolStrip_MouseMove(object sender, MouseEventArgs e)
        {
            var currentItem = GetHoveredItemRectangle(e.Location);

            // If item didn't change, raise move event
            if (_hoveredItem.Item1 == currentItem.Item1)
            {
                _hoveredItem.Item2?.OnMouseMove(e);
                return;
            }

            // Raise leave event of previous item
            _hoveredItem.Item2?.OnMouseLeave(e);

            // Raise enter event of current item
            currentItem.Item2?.OnMouseEnter(e);

            _hoveredItem = currentItem;
        }

        private (RectangleF, ToolStripItem) GetHoveredItemRectangle(PointF location)
        {
            var rects = GetItemInformation();
            return rects.FirstOrDefault(x => x.Item1.Contains(location));
        }

        private (RectangleF, ToolStripItem)[] GetItemInformation()
        {
            // TODO: Cache item rectangles?
            var rects = new List<(RectangleF, ToolStripItem)>();

            var position = new PointF(Padding.Left, Padding.Right);
            foreach (var item in Items)
            {
                rects.Add((new RectangleF(position, new SizeF(item.Width, ToolStripItem.Height)), item));
                position = new PointF(position.X + item.Width + Spacing, position.Y);
            }

            return rects.ToArray();
        }
    }

    public class ToolStripItemCollection : Collection<ToolStripItem>
    {
        private readonly ToolStrip _parent;

        public ToolStripItemCollection(ToolStrip parent)
        {
            _parent = parent;
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            _parent.Invalidate();
        }

        protected override void InsertItem(int index, ToolStripItem item)
        {
            base.InsertItem(index, item);

            item?.RegisterParent(_parent);
            _parent.Invalidate();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            _parent.Invalidate();
        }

        protected override void SetItem(int index, ToolStripItem item)
        {
            base.SetItem(index, item);

            item?.RegisterParent(_parent);
            _parent.Invalidate();
        }

        public new void Add(ToolStripItem item)
        {
            base.Add(item);
            item?.RegisterParent(_parent);
        }
    }

    public class SplitterToolStripItem : ToolStripItem
    {
        public override float Width => 1;
        public override float Paint(Graphics g, PointF drawLocation)
        {
            g.FillRectangle(Color.FromArgb(0xbd, 0xbd, 0xbd), new RectangleF(new PointF(drawLocation.X, drawLocation.Y + (Height - 16) / 2), new SizeF(Width, 16)));
            return Width;
        }
    }

    public class ButtonToolStripItem : ToolStripItem
    {
        private bool _isHovering;
        private bool _isClicked;
        private bool _isEnabled;

        public Command Command { get; set; }

        public bool Enabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                ParentToolStrip?.Invalidate();
            }
        }

        public override float Width => Height;

        public ButtonToolStripItem()
        {
            _isEnabled = true;

            MouseEnter += ButtonToolStripItem_MouseEnter;
            MouseLeave += ButtonToolStripItem_MouseLeave;
            MouseDown += ButtonToolStripItem_MouseDown;
            MouseUp += ButtonToolStripItem_MouseUp;
        }

        public override float Paint(Graphics g, PointF drawLocation)
        {
            if (Command.Image == null)
                return 0;

            // If clicked use darker colors
            if (_isEnabled && _isClicked)
            {
                g.FillRectangle(Color.FromArgb(0x80, 0xbc, 0xeb), new RectangleF(drawLocation, new SizeF(Width, Height)));
                g.DrawRectangle(new Pen(Color.FromArgb(0, 0x78, 0xd7)), new RectangleF(drawLocation, new SizeF(Width, Height)));
            }

            // If only hovering use lighter colors
            if (_isEnabled && !_isClicked && _isHovering)
            {
                g.FillRectangle(Color.FromArgb(0xb3, 0xd7, 0xf3), new RectangleF(drawLocation, new SizeF(Width, Height)));
                g.DrawRectangle(new Pen(Color.FromArgb(0, 0x78, 0xd7)), new RectangleF(drawLocation, new SizeF(Width, Height)));
            }

            g.DrawImage(_isEnabled ? Command.Image : ToGreyScale(Command.Image),
                new RectangleF(new PointF(drawLocation.X + (Width - 16) / 2, drawLocation.Y + (Height - 16) / 2), new SizeF(16, 16)));
            return Width;
        }

        private void ButtonToolStripItem_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isEnabled)
            {
                Command?.Execute();
            }

            _isClicked = false;
            ParentToolStrip?.Invalidate();
        }

        private void ButtonToolStripItem_MouseDown(object sender, MouseEventArgs e)
        {
            _isClicked = true;
            ParentToolStrip?.Invalidate();
        }

        private void ButtonToolStripItem_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHovering = false;
            _isClicked = false;
            ParentToolStrip?.Invalidate();
        }

        private void ButtonToolStripItem_MouseEnter(object sender, MouseEventArgs e)
        {
            _isHovering = true;
            _isClicked = (e.Buttons & MouseButtons.Primary) != 0;
            ParentToolStrip?.Invalidate();
        }

        private Image ToGreyScale(Image image)
        {
            var bitmap = (Bitmap)image;
            var clonedImage = bitmap.Clone();

            var data = clonedImage.Lock();
            data.SetPixels(data.GetPixels().Select(c =>
            {
                var grey = c.R * 0.2126f + c.G * 0.7152f + c.B * 0.0722f;
                return new Color(grey, grey, grey, c.A * 0.5f);
            }));
            data.Dispose();

            return clonedImage;
        }
    }

    public abstract class ToolStripItem : IMouseInputSource
    {
        public const float Height = 24;

        public abstract float Width { get; }

        protected ToolStrip ParentToolStrip { get; private set; }

        public event EventHandler<MouseEventArgs> MouseUp;
        public event EventHandler<MouseEventArgs> MouseMove;
        public event EventHandler<MouseEventArgs> MouseEnter;
        public event EventHandler<MouseEventArgs> MouseLeave;
        public event EventHandler<MouseEventArgs> MouseDown;
        public event EventHandler<MouseEventArgs> MouseDoubleClick;
        public event EventHandler<MouseEventArgs> MouseWheel;

        public abstract float Paint(Graphics g, PointF drawLocation);

        internal void RegisterParent(ToolStrip parent)
        {
            ParentToolStrip = parent;
        }

        internal void OnMouseMove(MouseEventArgs e)
        {
            MouseMove?.Invoke(this, e);
        }

        internal void OnMouseEnter(MouseEventArgs e)
        {
            MouseEnter?.Invoke(this, e);
        }

        internal void OnMouseLeave(MouseEventArgs e)
        {
            MouseLeave?.Invoke(this, e);
        }

        internal void OnMouseDown(MouseEventArgs e)
        {
            MouseDown?.Invoke(this, e);
        }

        internal void OnMouseUp(MouseEventArgs e)
        {
            MouseUp?.Invoke(this, e);
        }

        internal void OnMouseDoubleClick(MouseEventArgs e)
        {
            MouseDoubleClick?.Invoke(this, e);
        }

        internal void OnMouseWheel(MouseEventArgs e)
        {
            MouseWheel?.Invoke(this, e);
        }
    }
}
