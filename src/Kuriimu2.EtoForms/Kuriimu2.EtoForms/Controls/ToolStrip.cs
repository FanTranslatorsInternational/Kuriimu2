using System.Collections.ObjectModel;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace Kuriimu2.EtoForms.Controls
{
    
    public class ToolStrip : Panel
    {
        internal readonly StackLayout Layout;
        
        // ReSharper disable once CollectionNeverQueried.Global (XXX caused by awkward architecture)
        public ToolStripItemCollection Items { get; }
        public int ItemHeight { get; }

        public ToolStrip(int itemHeight = 24)
        {
            ItemHeight = itemHeight;
            Content = Layout = new StackLayout { Orientation = Orientation.Horizontal};
            Items = new ToolStripItemCollection(this);
        }
    }

    //XXX this is kind of an awkward proxy class, is there a better solution?
    //  We want to allow fancy initializing, and directly pass the elements into the layout, as well as register the parent
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
            _parent.Layout.Items.Clear();
            _parent.Invalidate();
        }

        protected override void InsertItem(int index, ToolStripItem item)
        {
            base.InsertItem(index, item);
            _parent.Layout.Items.Insert(index, item);
            item?.RegisterParent(_parent);
            _parent.Invalidate();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            _parent.Layout.Items.RemoveAt(index);
            _parent.Invalidate();
        }

        protected override void SetItem(int index, ToolStripItem item)
        {
            base.SetItem(index, item);
            _parent.Layout.Items[index] = item;
            item?.RegisterParent(_parent);
            _parent.Invalidate();
        }

        public new void Add(ToolStripItem item)
        {
            base.Add(item);
            _parent.Layout.Items.Add(item);
            item?.RegisterParent(_parent);
        }
    }

    public class SplitterToolStripItem : ToolStripItem
    {
        public override int Width => Padding.Horizontal + 1;

        public SplitterToolStripItem()
        {
            Padding = new Padding(5, 2);
        }

        protected override void DoPaint(Graphics g)
        {
            g.FillRectangle(Color.FromArgb(0xbd, 0xbd, 0xbd), Padding.Left, Padding.Top, 1, Height - Padding.Vertical);
        }
    }

    public class ButtonToolStripItem : ToolStripItem
    {
        private bool _isHovering;
        private bool _isClicked;

        public override int Width => Height;

        public Command Command { get; set; }

        public ButtonToolStripItem()
        {
            MouseEnter += ButtonToolStripItem_MouseEnter;
            MouseLeave += ButtonToolStripItem_MouseLeave;
            MouseDown += ButtonToolStripItem_MouseDown;
            MouseUp += ButtonToolStripItem_MouseUp;
        }

        protected override void DoPaint(Graphics g)
        {
            if (Command.Image == null)
                return;

            var rect = new RectangleF(0, 0, Width, Height);

            // If clicked use darker colors
            if (Enabled && _isClicked)
            {
                g.FillRectangle(Color.FromArgb(0x80, 0xbc, 0xeb), rect);
                g.DrawRectangle(new Pen(Color.FromArgb(0, 0x78, 0xd7)), rect);
            }

            // If only hovering use lighter colors
            if (Enabled && !_isClicked && _isHovering)
            {
                g.FillRectangle(Color.FromArgb(0xb3, 0xd7, 0xf3), rect);
                g.DrawRectangle(new Pen(Color.FromArgb(0, 0x78, 0xd7)), rect);
            }

            g.DrawImage(Enabled ? Command.Image : ToGreyScale(Command.Image),
                new RectangleF(new PointF((Width - 16) / 2F, (Height - 16) / 2F), new SizeF(16, 16)));
        }

        #region Events
        private void ButtonToolStripItem_MouseUp(object sender, MouseEventArgs e)
        {
            if (Enabled)
            {
                Command?.Execute();
            }

            _isClicked = false;
            Invalidate();
        }

        private void ButtonToolStripItem_MouseDown(object sender, MouseEventArgs e)
        {
            _isClicked = true;
            Invalidate();
        }

        private void ButtonToolStripItem_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHovering = false;
            _isClicked = false;
            Invalidate();
        }

        private void ButtonToolStripItem_MouseEnter(object sender, MouseEventArgs e)
        {
            _isHovering = true;
            _isClicked = (e.Buttons & MouseButtons.Primary) != 0;
            Invalidate();
        }
        #endregion

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

    public abstract class ToolStripItem : Drawable
    {
        protected ToolStrip ParentToolStrip { get; private set; }
        
        public new abstract int Width { get; }

        protected ToolStripItem()
        {
            Paint += (_, ev) => DoPaint(ev.Graphics);
        }

        protected abstract void DoPaint(Graphics g);

        internal void RegisterParent(ToolStrip parent)
        {
            ParentToolStrip = parent;
            Size = new Size(Width, ParentToolStrip.ItemHeight);
        }
    }
}
