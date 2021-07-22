using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Controls
{
    class HexBox : Drawable, IScrollableContent
    {
        private const int LinePadding_ = 5;
        private const int BytePadding_ = 3;
        private const int ByteGroupPadding_ = 10;
        private const int PositionWidthPadding_ = 7;

        private Font _monoFont;
        private float _monoHeight;
        private int _lineHeight;

        private Stream _data;

        private Color SideBarBackground => Themer.GetTheme().HexSidebarBackColor;
        private Color ByteBackground1 => Themer.GetTheme().HexByteBack1Color;
        #region Properties

        #region Backing fields

        private int _fontSize = 10;
        private int _byteGroup = 4;
        private int _bytesPerLine = 16;
        private bool _showLinePositions = true;
        private bool _showHeader = true;

        #endregion

        public int FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                Invalidate();
            }
        }

        public int ByteGroup
        {
            get => _byteGroup;
            set
            {
                _byteGroup = value;
                Invalidate();
            }
        }

        public int BytesPerLine
        {
            get => _bytesPerLine;
            set
            {
                _bytesPerLine = value;
                Invalidate();
            }
        }

        public bool ShowLinePositions
        {
            get => _showLinePositions;
            set
            {
                _showLinePositions = value;
                Invalidate();
            }
        }

        public bool ShowHeader
        {
            get => _showHeader;
            set
            {
                _showHeader = value;
                Invalidate();
            }
        }

        #endregion

        public HexBox()
        {
            UpdateFontInformation(_fontSize);
        }

        public void LoadStream(Stream input)
        {
            _data = input;

            var headerHeight = _monoFont.MeasureString("A").Height;
            var lineStartY = (ShowHeader ? headerHeight : 0) + LinePadding_;

            // TODO: Calculate actual width
            var lines = input.Length / BytesPerLine + (input.Length % BytesPerLine > 0 ? 1 : 0);
            Size = new Size(0, (int)(lines * _lineHeight + lineStartY));
        }

        private int GetPositionWidth(bool isLongPosition)
        {
            return (int)GetFontWidth(_monoFont, isLongPosition ? "CCCCCCCC" : "CCCCCCCCCCCCCCCC");
        }

        #region Font methods

        private void UpdateFontInformation(float size)
        {
            _monoFont = CreateMonoFont(size);
            _monoHeight = GetFontHeight(_monoFont);
            _lineHeight = (int)Math.Ceiling(_monoHeight + LinePadding_);
        }

        private static float GetFontHeight(Font font)
        {
            return font.MeasureString("A").Height;
        }

        private static float GetFontWidth(Font font, string text)
        {
            return font.MeasureString(text).Width;
        }

        private Font CreateMonoFont(float size)
        {
            return new Font(FontFamilies.Monospace, size);
        }

        #endregion

        #region Painting

        void IScrollableContent.Paint(RelativeGraphics g, RectangleF clipRectangle)
        {
            PaintInternal(g, clipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintInternal(new RelativeGraphics(e.Graphics, PointF.Empty), e.ClipRectangle);
        }

        private void PaintInternal(RelativeGraphics g, RectangleF clipRectangle)
        {
            var headerHeight = _monoFont.MeasureString("A").Height;
            var lineStartY = (ShowHeader ? headerHeight : 0) + LinePadding_;

            // Read buffer
            var startLine = (long)((clipRectangle.Y == 0 ? 0 : clipRectangle.Y + lineStartY) / _lineHeight);
            var totalLines = (float)Math.Ceiling((clipRectangle.Height - lineStartY) / _lineHeight +
                             ((clipRectangle.Height - lineStartY) % _lineHeight > 0 ? 1 : 0));

            var dataBuffer = new byte[(int)(BytesPerLine * totalLines)];
            _data.Position = startLine * BytesPerLine;
            _data.Read(dataBuffer, 0, dataBuffer.Length);

            // Draw background colors
            var positionWidth = GetPositionWidth((startLine + (long)totalLines) * BytesPerLine < int.MaxValue) + PositionWidthPadding_ * 2;
            var lineStartX = ShowLinePositions ? positionWidth + 5 : 0;
            var monoCenteredY = (int)((_lineHeight - Math.Ceiling(_monoHeight)) / 2);
            var byteWidth = GetFontWidth(_monoFont, "FF") + BytePadding_;

            if (ShowLinePositions)
                g.FillRectangle(SideBarBackground, clipRectangle.X, clipRectangle.Y + lineStartY, positionWidth, clipRectangle.Height - lineStartY);

            if (ShowHeader)
                g.FillRectangle(SideBarBackground, clipRectangle.X + lineStartX, clipRectangle.Y, clipRectangle.Width - lineStartX, headerHeight);

            var byteX = (float)lineStartX;
            var byteY = clipRectangle.Y + lineStartY;
            for (var i = 0; i < BytesPerLine; i++)
            {
                if (i > 0 && i % ByteGroup == 0)
                    byteX += ByteGroupPadding_;

                if (i % 2 == 1)
                    g.FillRectangle(ByteBackground1, byteX, byteY, byteWidth, clipRectangle.Height);

                byteX += byteWidth;
            }

            // Draw lines
            for (var l = 0; l < totalLines; l++)
            {
                if (l >= dataBuffer.Length / BytesPerLine)
                    break;

                byteX = lineStartX;
                for (var i = 0; i < BytesPerLine; i++)
                {
                    if (i >= Math.Min(BytesPerLine, dataBuffer.Length - l * BytesPerLine))
                        break;

                    if (i > 0 && i % ByteGroup == 0)
                        byteX += ByteGroupPadding_;

                    var bytePosition = new PointF(byteX, byteY + monoCenteredY);
                    g.DrawText(_monoFont, new SolidBrush(Themer.GetTheme().AltColor), bytePosition, $"{dataBuffer[l * BytesPerLine + i]:X2}");

                    byteX += byteWidth;
                }

                byteY += _lineHeight;
            }

            // Draw line positions
            if (ShowLinePositions)
            {
                byteY = clipRectangle.Y + lineStartY;
                for (var l = 0; l < totalLines; l++)
                {
                    var linePosition = new PointF(PositionWidthPadding_, byteY + monoCenteredY);
                    g.DrawText(_monoFont, new SolidBrush(Themer.GetTheme().AltColor), linePosition, $"{(startLine + l) * BytesPerLine:X8}");

                    byteY += _lineHeight;
                }

                g.DrawLine(Themer.GetTheme().ControlColor, new PointF(clipRectangle.X + positionWidth, clipRectangle.Y), new PointF(clipRectangle.X + positionWidth, clipRectangle.Y + clipRectangle.Height));
            }

            // Draw header
            if (ShowHeader)
            {
                byteX = lineStartX;
                for (var i = 0; i < BytesPerLine; i++)
                {
                    if (i > 0 && i % ByteGroup == 0)
                        byteX += ByteGroupPadding_;

                    var bytePosition = new PointF(byteX, clipRectangle.Y + monoCenteredY);
                    g.DrawText(_monoFont, new SolidBrush(Themer.GetTheme().AltColor), bytePosition, $"{i:X2}");

                    byteX += byteWidth;
                }

                g.DrawLine(Themer.GetTheme().ControlColor, new PointF(clipRectangle.X, clipRectangle.Y + (int)headerHeight), new PointF(clipRectangle.X + clipRectangle.Width, clipRectangle.Y + (int)headerHeight));
            }
        }

        #endregion
    }
}
