using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Kanvas;
using Kuriimu2.EtoForms.Extensions;
using Kuriimu2.EtoForms.Support;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace Kuriimu2.EtoForms.Controls
{
    class PaletteView : Drawable
    {
        private IList<Color> _palette;
        private int _selectedIndex = -1;

        public event EventHandler<ChoosingColorEventArgs> ChoosingColor;
        public event EventHandler<PaletteChangedEventArgs> PaletteChanged;

        public IList<Color> Palette
        {
            get => _palette;
            set
            {
                _palette = value?.ToArray();
                Invalidate();
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw palette image
            if (_palette == null)
                return;

            var dimPalette = GetPaletteDimension();
            var paletteImg = _palette.ToBitmap(new Size(dimPalette, dimPalette)).ToEto();

            e.Graphics.ImageInterpolation = ImageInterpolation.None;
            e.Graphics.DrawImage(paletteImg, e.ClipRectangle);

            // Draw selected index
            if (_selectedIndex < 0 || _selectedIndex >= _palette.Count)
                return;

            var width = e.ClipRectangle.Width / dimPalette;
            var height = e.ClipRectangle.Height / dimPalette;

            var posX = _selectedIndex % dimPalette * width;
            var posY = _selectedIndex / dimPalette * height;

            var paletteColor = Palette[_selectedIndex];
            var borderColor = paletteColor.GetBrightness() <= 0.49 ? Themer.GetTheme().MainColor : Themer.GetTheme().AltColor;

            e.Graphics.DrawRectangle(new Pen(borderColor, 2), new RectangleF(new PointF(posX, posY), new SizeF(width, height)));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Buttons.HasFlag(MouseButtons.Alternate))
            {
                // Select index
                SelectIndex(e.Location);
                if (_selectedIndex < 0 || _selectedIndex >= _palette.Count)
                    return;

                Invalidate();

                // Invoke color choosing event
                var args = new ChoosingColorEventArgs();
                ChoosingColor?.Invoke(this, args);

                if (args.Cancel)
                    return;

                // Invoke changed palette event
                _palette[_selectedIndex] = args.Result;
                PaletteChanged?.Invoke(this, new PaletteChangedEventArgs(_selectedIndex, args.Result));

                Invalidate();
            }
        }

        private void SelectIndex(PointF location)
        {
            var dimPalette = GetPaletteDimension();

            var width = (float)Size.Width / dimPalette;
            var height = (float)Size.Height / dimPalette;

            var x = (int)(location.X / width);
            var y = (int)(location.Y / height);

            _selectedIndex = y * dimPalette + x;
        }

        private int GetPaletteDimension()
        {
            return (int)Math.Ceiling(Math.Sqrt(_palette.Count));
        }
    }

    class ChoosingColorEventArgs : EventArgs
    {
        public Color Result { get; set; }

        public bool Cancel { get; set; }
    }

    class PaletteChangedEventArgs : EventArgs
    {
        public int Index { get; }

        public Color NewColor { get; }

        public PaletteChangedEventArgs(int index, Color color)
        {
            Index = index;
            NewColor = color;
        }
    }
}
